using yssystem_solution.Data;
using yssystem_solution.Entities;
using yssystem_solution.Services;

namespace yssystem_solution
{
    public partial class EquipmentMonitorForm : Form
    {
        #region 필드 / 생성자

        private readonly EquipmentService _service = new EquipmentService();

        // 그리드 재바인딩 중에는 SelectionChanged 처리를 막기 위한 플래그
        private bool _isBinding = false;

        // 마지막으로 상태 콤보박스를 맞췄을 때의 설비와 상태
        // (설비나 실제 상태가 바뀔 때만 콤보박스를 맞추고, 그대로면 사용자가 고르던 값을 유지하기 위함)
        private string? _lastLogEquipId;
        private string? _lastLogStatus;

        // 시뮬레이터 정지 신호 (null이면 정지 상태)
        private CancellationTokenSource? _cts;

        // 시뮬레이터 수집 대상 설비 / 주기
        private const string SimEquipId = "EQP_001";
        private const int SimIntervalMs = 1500;

        // 수집 데이터 저장 실패 시 재시도 (DB 오류일 때만, 최초 1회 + 재시도 2회)
        private const int SaveMaxAttempts = 3;
        private const int SaveRetryDelayMs = 1000;

        // 폴링: 다른 곳(SSMS 등)에서 바뀐 DB 내용도 화면에 반영하기 위해 주기적으로 변경 여부 확인
        private const int PollIntervalMs = 3000;
        private readonly System.Windows.Forms.Timer _pollTimer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };

        // 마지막으로 화면을 그렸을 때의 DB 상태 (폴링 때 이 값과 다르면 다시 그림)
        private (int EquipChecksum, long LastLogId) _lastDataVersion;

        // 폴링 확인이 진행 중인지 (앞 확인이 끝나기 전에 다음 Tick이 오면 건너뛰기 위함)
        private bool _isPolling = false;

        // 목록 갱신은 한 번에 하나만 실행 (진행 중에 또 요청이 오면 새로 시작하지 않고, 끝난 뒤 한 번 더 갱신)
        private Task? _listRefreshTask;     // 진행 중인 목록 갱신 (null이면 진행 중 아님)
        private bool _listRefreshAgain;     // 진행 중에 갱신 요청이 또 들어왔는지

        // 로그 조회 요청 번호 (설비를 빠르게 바꿔 클릭했을 때, 늦게 도착한 이전 설비의 로그는 버리기 위함)
        private int _logLoadSeq = 0;

        // 다음 목록 갱신 때 꼭 선택해야 하는 설비 (등록/상태변경/판정 직후의 설비)
        private string? _pendingSelectEquipId;

        private const string FormTitle = "설비 FDC 모니터링";

        public EquipmentMonitorForm()
        {
            InitializeComponent();

            Load += EquipmentMonitorForm_Load;
            FormClosing += EquipmentMonitorForm_FormClosing;

            btnAdd.Click += btnAdd_Click;
            btnChangeStatus.Click += btnChangeStatus_Click;
            btnCheck.Click += btnCheck_Click;
            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;

            dgvEquipment.SelectionChanged += dgvEquipment_SelectionChanged;
            dgvEquipment.DataBindingComplete += dgvEquipment_DataBindingComplete;

            _pollTimer.Tick += pollTimer_Tick;
        }

        #endregion

        #region 화면 초기화 / 종료

        // 화면 초기화: 콤보박스/그리드 설정 후 설비 목록 조회
        // 추가하면 좋을거 object[] { "LINE_A", "LINE_B", "LINE_C" } 같은거를 enum에서 가져오도록 바꾸는거
        private async void EquipmentMonitorForm_Load(object? sender, EventArgs e)
        {
            cboLine.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLine.Items.AddRange(new object[] { "LINE_A", "LINE_B", "LINE_C" });
            cboLine.SelectedIndex = 0;

            cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStatus.Items.AddRange(new object[] { "IDLE", "RUN", "DOWN" });
            cboStatus.SelectedIndex = 0;

            cboParam.DropDownStyle = ComboBoxStyle.DropDownList;
            cboParam.Items.AddRange(new object[] { "TEMP", "PRESS" });
            cboParam.SelectedIndex = 0;

            // 시뮬레이터의 default 상태는 정지, 시작 버튼만 활성화
            btnStop.Enabled = false;
            lblSimStatus.Text = "시뮬레이터: 정지";

            InitGrid(dgvEquipment);
            InitGrid(dgvLogs);

            try
            {
                await LoadEquipmentListAsync();
            }
            finally
            {
                // 처음 Load 이후 폴링 시작 (시작할 때 DB가 끊겨 있어도 폴링은 시작해서, DB가 살아나면 자동으로 화면을 채우게 함)
                if (!IsDisposed)
                {
                    _pollTimer.Start();
                }
            }
        }

        // 폼을 닫을 때 폴링과 수집 루프도 함께 정지
        private void EquipmentMonitorForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            StopCollector();
        }

        // 그리드 공통 설정 (읽기 전용, 한 행 전체 선택)
        private void InitGrid(DataGridView dgv)
        {
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.RowHeadersVisible = false;
        }

        #endregion

        #region 조회 / 바인딩

        // 설비 목록 갱신 요청 (폴링, 버튼, 시뮬레이터 모두 이 메서드로 요청)
        // selectEquipId: 다시 그린 뒤 꼭 선택할 설비 (없으면 그 시점에 선택돼 있던 설비를 유지)
        // - 진행 중인 갱신이 없으면: 새로 시작
        // - 진행 중이면: 새로 시작하지 않고 "끝나면 한 번 더" 표시만 한 뒤, 진행 중인 갱신이 끝날 때까지 같이 기다림
        //   → DB에는 목록 조회가 항상 하나만 나가고, 요청이 계속 와도 매번 끝까지 그리므로 화면이 멈춰 있지 않음
        // ※ UI 스레드에서만 호출됨 (Worker는 UpdateMonitoringUI로 UI 스레드에 넘어온 뒤 호출) → 잠금(lock) 불필요
        private Task LoadEquipmentListAsync(string? selectEquipId = null)
        {
            //  진행 중이면 새로 보내지 않고 "끝나면 한 번 더"로 합침
            // -> 조회는 항상 하나만, 매번 끝까지 그리므로 기아도 요청 쌓임도 없음
            if (selectEquipId != null)
            {
                _pendingSelectEquipId = selectEquipId;
            }

            if (_listRefreshTask != null)
            {
                _listRefreshAgain = true;
                return _listRefreshTask;
            }

            Task task = RunListRefreshAsync();

            // 시작하자마자 끝나 버린 경우(첫 await 전에 예외 등)는 진행 중으로 기록하지 않음
            if (!task.IsCompleted)
            {
                _listRefreshTask = task;
            }

            return task;
        }

        // 목록 갱신 반복: 갱신하는 동안 요청이 또 들어왔으면 한 번 더 (그동안 온 요청 여러 개는 1번으로 합쳐짐)
        // 오류가 나면 반복을 멈추고 예외는 기다리던 쪽(버튼/폴링/시뮬레이터)으로 전달 → 다음 폴링에서 다시 시도됨
        private async Task RunListRefreshAsync()
        {
            try
            {
                do
                {
                    _listRefreshAgain = false;
                    await RefreshEquipmentListOnceAsync();
                }
                while (_listRefreshAgain && !IsDisposed);
            }
            finally
            {
                _listRefreshTask = null;
            }
        }

        // 설비 목록 1회 조회 후 마스터 그리드에 바인딩
        private async Task RefreshEquipmentListOnceAsync()
        {
            // 이번 갱신에서 선택할 설비 예약을 가져감
            // DB 조회 "전"에 가져가야 함: 조회 도중 들어온 예약(예: 방금 등록한 설비)은
            // 이번 조회 결과에 없을 수 있으므로, 필드에 남겨 두었다가 다음 갱신에서 사용
            string? requestedEquipId = _pendingSelectEquipId;
            _pendingSelectEquipId = null;

            // DB 조회 (기다리는 동안 화면이 멈추지 않음)
            // 목록을 읽기 "전"의 DB 상태를 먼저 받아둠 (읽는 사이에 바뀐 건 다음 폴링에서 잡힘)
            var version = await _service.GetDataVersionAsync();
            var list = await _service.GetEquipmentListAsync();

            // 기다리는 사이 창이 닫혔으면 그리지 않음
            if (IsDisposed)
            {
                return;
            }

            // DB 조회에 성공 -> DB 상태 표시를 정상으로
            SetDbStatus(true);

            // 다시 선택할 설비는 지금 정함 (requestedEquipId가 null이아니면 그대로 사용 null이면 지금 선택된 EquipId가 들어감)
            // 즉, 처음 Load 때에는 null로 기본 동작으로 첫 행이 선택됨
            string? targetEquipId = requestedEquipId ?? GetSelectedEquipment()?.EquipId;

            // 그리드에 채우기
            try
            {
                _isBinding = true;
                _lastDataVersion = version;

                dgvEquipment.DataSource = list;

                dgvEquipment.Columns["EquipId"]!.HeaderText = "설비ID";
                dgvEquipment.Columns["EquipName"]!.HeaderText = "설비명";
                dgvEquipment.Columns["LineName"]!.HeaderText = "라인";
                dgvEquipment.Columns["Status"]!.HeaderText = "상태";
                dgvEquipment.Columns["CreateDt"]!.HeaderText = "생성일시";
                dgvEquipment.Columns["CreateDt"]!.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";

                if (targetEquipId != null)
                {
                    foreach (DataGridViewRow row in dgvEquipment.Rows)
                    {
                        if (row.Cells["EquipId"].Value as string == targetEquipId)
                        {
                            dgvEquipment.CurrentCell = row.Cells["EquipId"];
                            break;
                        }
                    }
                }
            }
            finally
            {
                // 오류가 나도 플래그는 반드시 해제
                _isBinding = false;
            }

            // 선택된 설비의 로그도 함께 갱신
            await LoadSelectedEquipmentLogsAsync();
        }

        // 마스터 그리드에서 선택된 설비의 로그를 디테일 그리드에 바인딩
        private async Task LoadSelectedEquipmentLogsAsync()
        {
            int seq = ++_logLoadSeq;

            Equipment? selected = GetSelectedEquipment();
            if (selected == null)
            {
                dgvLogs.DataSource = null;
                return;
            }

            // 다른 설비를 선택했거나, 같은 설비라도 실제 상태가 바뀌었을 때만 콤보박스를 맞춤
            // (상태가 그대로면 폴링/시뮬레이터가 화면을 다시 그려도 사용자가 고르던 값을 유지)
            if (selected.EquipId != _lastLogEquipId || selected.Status != _lastLogStatus)
            {
                _lastLogEquipId = selected.EquipId;
                _lastLogStatus = selected.Status;

                if (selected.Status != null && cboStatus.Items.Contains(selected.Status))
                {
                    cboStatus.SelectedItem = selected.Status;
                }
            }

            var logs = await _service.GetLogsByEquipIdAsync(selected.EquipId);

            // 기다리는 사이 다른 설비를 클릭했거나(더 최근 요청) 창이 닫혔으면 이 결과는 버림
            if (seq != _logLoadSeq || IsDisposed)
            {
                return;
            }

            SetDbStatus(true);
            dgvLogs.DataSource = logs;

            dgvLogs.Columns["EquipId"]!.Visible = false;
            dgvLogs.Columns["LogId"]!.HeaderText = "LogId";
            dgvLogs.Columns["LogType"]!.HeaderText = "Type";
            dgvLogs.Columns["LogMsg"]!.HeaderText = "Msg";
            dgvLogs.Columns["OccurDt"]!.HeaderText = "Time";
            dgvLogs.Columns["OccurDt"]!.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
            dgvLogs.Columns["LogId"]!.FillWeight = 0.5f;
            dgvLogs.Columns["LogType"]!.FillWeight = 0.7f;
            dgvLogs.Columns["LogMsg"]!.FillWeight = 2.0f;
            dgvLogs.Columns["OccurDt"]!.FillWeight = 1.5f;
        }

        // CurrentRow의 원본 객체(DataBoundItem, object 타입)를 Equipment로 변환
        private Equipment? GetSelectedEquipment()
        {
            return dgvEquipment.CurrentRow?.DataBoundItem as Equipment;
        }

        // 설비 선택 변경 시 로그 즉시 갱신
        private async void dgvEquipment_SelectionChanged(object? sender, EventArgs e)
        {
            if (_isBinding)
            {
                return;
            }

            await LoadSelectedEquipmentLogsAsync();
        }

        // 그리드에 데이터가 채워질 때마다 행 색상 다시 적용 (등록/상태변경/판정/폴링/시뮬레이터 갱신 모두 해당)
        // 이러면 리소스 낭비가 심할까? -> 설비 수가 많지 않으면 괜찮다고 판단, 필요하다면 화면에 보이는 줄을 그릴 때만 색 정하게 하는 방식들으로 해결 가능할 것...?
        private void dgvEquipment_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            UpdateGridColors();
        }

        // DOWN 설비 행을 빨간색(LightCoral)으로 강조
        private void UpdateGridColors()
        {
            foreach (DataGridViewRow row in dgvEquipment.Rows)
            {
                if (row.Cells["Status"].Value as string == "DOWN")
                {
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                    row.DefaultCellStyle.SelectionBackColor = Color.IndianRed;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.Empty;
                    row.DefaultCellStyle.SelectionBackColor = Color.Empty;
                }
            }
        }

        #endregion

        #region 폴링 (외부 변경 감지)

        // 주기적으로 DB 상태를 가볍게 확인해서, 바뀌었을 때만 화면을 다시 그림
        // 지금은 적은 규모이기 때문에 폴링으로 구현, 카프카를 쓴다면 DB에 저장하기 때문에 수신받는 즉시, 리프레쉬 하는 방향으로 구현 가능
        // 그럼 DB의 상태가 변화하는걸 알리면 리프레쉬하는건? -> Query Notification를 쓰면 된다고는 하는데 복잡해질 것 같아 지금 단계에선 배제
        private async void pollTimer_Tick(object? sender, EventArgs e)
        {
            // 앞 확인이 아직 진행 중이면 이번 주기는 건너뜀 (DB가 느릴 때 확인이 겹겹이 쌓이지 않게)
            if (_isPolling)
            {
                return;
            }

            _isPolling = true;
            try
            {
                // 확인 조회를 비동기로 (DB가 끊겨 연결을 기다리는 동안에도 화면이 멈추지 않게)
                var version = await _service.GetDataVersionAsync();

                // 여기부터는 다시 UI 스레드. 기다리는 사이 창이 닫혔으면 아무것도 하지 않음
                if (IsDisposed)
                {
                    return;
                }

                SetDbStatus(true);

                if (version != _lastDataVersion)
                {
                    // 현재 선택한 설비를 유지한 채 목록/로그 갱신
                    await LoadEquipmentListAsync();
                }
            }
            catch (Exception ex)
            {
                // 자동 반복 작업이라 오류 창은 띄우지 않고, DB 문제면 제목 표시줄로만 알림 (다음 주기에 다시 시도)
                ReportBackgroundError("폴링", ex);
            }
            finally
            {
                // 오류가 나도 반드시 해제 (안 하면 폴링이 계속 건너뛰어짐)
                _isPolling = false;
            }
        }

        #endregion

        #region 설비 등록 / 상태 변경

        // 버튼 작업 중 표시: 작업 버튼을 비활성화해서 연타(중복 실행)를 막고, 마우스 커서를 대기 모양으로
        private void SetBusy(bool busy)
        {
            btnAdd.Enabled = !busy;
            btnChangeStatus.Enabled = !busy;
            btnCheck.Enabled = !busy;
            UseWaitCursor = busy;
        }

        // 신규 설비 등록 (입력값 검사는 서비스에서 하고, 화면은 결과만 안내)
        private async void btnAdd_Click(object? sender, EventArgs e)
        {
            var item = new Equipment
            {
                EquipId = txtEquipId.Text,
                EquipName = txtEquipName.Text,
                LineName = cboLine.SelectedItem as string ?? string.Empty
            };

            SetBusy(true);
            try
            {
                try
                {
                    await _service.AddEquipmentAsync(item);
                }
                catch (ArgumentException ex)
                {
                    // 빈 값 / 공백 입력
                    MessageBox.Show(ex.Message, "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                catch (InvalidOperationException ex)
                {
                    // 중복 ID (그 외 오류는 전역 처리기)
                    MessageBox.Show(ex.Message, "등록 불가", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (IsDisposed)
                {
                    return;
                }

                txtEquipId.Clear();
                txtEquipName.Clear();

                // 등록한 설비를 선택한 상태로 목록 갱신 (item.EquipId는 서비스에서 공백이 제거된 값)
                await LoadEquipmentListAsync(item.EquipId);
                MessageBox.Show("설비가 등록되었습니다.", "등록 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                // 성공/실패/오류 상관없이 버튼 원래대로
                if (!IsDisposed)
                {
                    SetBusy(false);
                }
            }
        }

        // 선택 설비 상태 변경 (UPDATE + 로그 INSERT 한 트랜잭션)
        private async void btnChangeStatus_Click(object? sender, EventArgs e)
        {
            Equipment? selected = GetSelectedEquipment();
            if (selected == null)
            {
                MessageBox.Show("상태를 변경할 설비를 선택하세요.", "선택 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? newStatus = cboStatus.SelectedItem as string;
            if (newStatus == null)
            {
                MessageBox.Show("변경할 상태를 선택하세요.", "선택 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newStatus == selected.Status)
            {
                MessageBox.Show($"이미 {newStatus} 상태입니다.", "상태 변경", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true);
            try
            {
                await _service.UpdateEquipmentStatusAsync(selected.EquipId, newStatus);

                if (IsDisposed)
                {
                    return;
                }

                // 두 그리드 모두 갱신 (변경한 설비 선택 유지)
                await LoadEquipmentListAsync(selected.EquipId);
            }
            finally
            {
                if (!IsDisposed)
                {
                    SetBusy(false);
                }
            }
        }

        #endregion

        #region FDC 판정 테스트 (과제 1-2 검증용)

        // 선택 설비 + 파라미터 + 입력값으로 FDC 판정 단독 실행
        private async void btnCheck_Click(object? sender, EventArgs e)
        {
            Equipment? selected = GetSelectedEquipment();
            if (selected == null)
            {
                MessageBox.Show("판정할 설비를 선택하세요.", "선택 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? paramName = cboParam.SelectedItem as string;
            if (paramName == null)
            {
                MessageBox.Show("파라미터를 선택하세요.", "선택 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(txtCheckVal.Text.Trim(), out double val))
            {
                MessageBox.Show("측정값을 숫자로 입력하세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                bool isAlarm;
                try
                {
                    isAlarm = await _service.CheckParameterAlarmAsync(selected.EquipId, paramName, val);
                }
                catch (InvalidOperationException ex)
                {
                    // 룰이 없는 설비/파라미터 등 업무 규칙 위반만 여기서 안내 (그 외 오류는 전역 처리기)
                    MessageBox.Show(ex.Message, "판정 불가", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (IsDisposed)
                {
                    return;
                }

                // 상태와 로그 변화를 바로 확인할 수 있게 갱신
                await LoadEquipmentListAsync(selected.EquipId);

                MessageBox.Show(isAlarm ? $"ALARM: {val} (설비 DOWN 처리)" : $"NORMAL: {val}", "판정 결과",
                    MessageBoxButtons.OK, isAlarm ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            finally
            {
                if (!IsDisposed)
                {
                    SetBusy(false);
                }
            }
        }

        #endregion

        #region 센서 시뮬레이터 (Worker 스레드)

        // [시뮬레이터 시작]: 백그라운드 스레드에서 수집 루프 구동
        private void btnStart_Click(object? sender, EventArgs e)
        {
            // 이미 실행 중이면 중복 시작 방지
            if (_cts != null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblSimStatus.Text = "시뮬레이터: 시작 중...";

            // UI 스레드가 멈추지 않도록 별도 Worker 스레드에서 실행
            Task.Run(() => RunCollectorAsync(token), token);
        }

        // [시뮬레이터 정지]: 루프에 정지 신호 전달
        private void btnStop_Click(object? sender, EventArgs e)
        {
            StopCollector();

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblSimStatus.Text = "시뮬레이터: 정지";
            lblSimStatus.ForeColor = SystemColors.ControlText;
        }

        // 정지 신호를 보내고 CancellationTokenSource 정리
        private void StopCollector()
        {
            if (_cts == null)
            {
                return;
            }

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        // 수집 루프 (Worker 스레드): 주기마다 난수 생성 → DB 저장 + FDC 판정 → UI 갱신 요청
        // Worker 스레드이므로 catch가 없을 경우 Program.cs의 전역 처리기로 안가서 원인 불명 오류 발생 가능
        private async Task RunCollectorAsync(CancellationToken token)
        {
            var rand = new Random();

            int failCount = 0;
            int unsavedCount = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 가상 센서값, 압력 생성
                    double temp = rand.Next(20, 95);
                    double press = Math.Round(rand.NextDouble() * 5 + 1, 1);
                    DateTime collectDt = DateTime.Now;   // 이벤트 발생 시각을 DB에 넣기

                    // TB_EQUIP_DATA INSERT + FDC 룰 검사 (이상 시 DOWN + ALARM 로그), DB 오류면 재시도
                    bool isFault = await SaveSensorDataWithRetryAsync(temp, press, collectDt, token);
                    failCount = 0;

                    string result = isFault ? "ALARM" : "NORMAL";
                    string unsavedText = unsavedCount > 0 ? $" | 미저장 누적 {unsavedCount}건" : "";

                    // 현재 메서드는 worker 스레드에서 실행 중이므로, UI 갱신은 UI 스레드로 위임
                    UpdateMonitoringUI(token, $"수집 중 | {collectDt:HH:mm:ss} | {SimEquipId} 온도 {temp}℃ / 압력 {press}bar → {result}{unsavedText}",
                        reloadGrid: isFault, dbOk: true);
                }
                catch (Exception ex)
                {
                    // 정지/종료 중에 난 오류는 무시하고 루프 종료
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    // 측정값이 저장되지 않았으므로 +1
                    failCount++;
                    unsavedCount++;

                    // Visual Studio Output 창에서 확인할 수 있도록 Debug기록
                    System.Diagnostics.Debug.WriteLine($"[수집 오류] {ex}");
                    // 빨간 글씨로 눈에 띄게 표시, DB 문제면 DB 상태 표시도 갱신 (룰 없음 같은 다른 오류면 DB 상태는 그대로)
                    UpdateMonitoringUI(token, $"저장 실패 (연속 {failCount}회, 미저장 누적 {unsavedCount}건) | {DateTime.Now:HH:mm:ss} | {ex.Message}",
                        reloadGrid: false, dbOk: DbErrorHelper.IsDbError(ex) ? false : null, isError: true);
                }

                // 주기만큼 대기 (정지 신호가 오면 즉시 깨어나 루프 종료)
                try
                {
                    await Task.Delay(SimIntervalMs, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        // 수집 데이터 저장 + FDC 판정, DB 오류(연결 끊김/시간 초과 등)면 잠시 뒤 다시 시도
        private async Task<bool> SaveSensorDataWithRetryAsync(double temp, double press, DateTime collectDt, CancellationToken token)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return await _service.SaveSensorDataAsync(SimEquipId, temp, press, collectDt);
                }
                catch (Exception ex) when (attempt < SaveMaxAttempts &&
                                           DbErrorHelper.IsDbError(ex) &&
                                           !token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"[저장 재시도 {attempt}/{SaveMaxAttempts - 1}] {ex.Message}");
                    UpdateMonitoringUI(token, $"저장 실패 | 재시도 중 ({attempt}/{SaveMaxAttempts - 1}) | {collectDt:HH:mm:ss} 측정값",
                        reloadGrid: false, dbOk: false, isError: true);

                    // 정지 신호가 오면 대기 중에도 즉시 OperationCanceledException으로 빠져나감
                    await Task.Delay(SaveRetryDelayMs, token);
                }
            }
        }

        #endregion

        #region 크로스 스레드 UI 갱신

        // Worker 스레드에서 호출되면 UI 스레드로 위임 (InvokeRequired + BeginInvoke 패턴)
        // async void: Worker가 부를 때는 await 전에 BeginInvoke 후 return하므로, 실제 await는 UI 스레드에서만 일어남
        // token: 이 요청을 보낸 수집 루프의 정지 신호 (정지된 루프가 늦게 보낸 요청을 걸러내기 위함)
        // dbOk: true = DB 정상, false = DB 오류, null = DB 상태 표시는 바꾸지 않음
        // isError: true면 라벨을 빨간 글씨로 표시 (저장 실패를 놓치지 않게)
        private async void UpdateMonitoringUI(CancellationToken token, string statusText, bool reloadGrid = true, bool? dbOk = null, bool isError = false)
        {
            if (InvokeRequired)
            {
                // 폼이 이미 닫혔으면 갱신하지 않음
                if (IsDisposed || !IsHandleCreated)
                {
                    return;
                }

                BeginInvoke(new Action(() => UpdateMonitoringUI(token, statusText, reloadGrid, dbOk, isError)));
                return;
            }

            // ---- 여기부터는 UI 스레드에서 실행됨 ----

            // 정지 버튼을 누른 뒤 늦게 도착한 갱신 요청은 무시
            // (_cts == null로 확인하면 정지 후 바로 다시 시작했을 때 이전 루프의 요청을 걸러내지 못하므로, 보낸 루프의 token으로 확인)
            if (token.IsCancellationRequested)
            {
                return;
            }

            lblSimStatus.Text = statusText;
            lblSimStatus.ForeColor = isError ? Color.Red : SystemColors.ControlText;

            if (dbOk != null)
            {
                SetDbStatus(dbOk.Value);
            }

            if (reloadGrid)
            {
                try
                {
                    // 현재 선택한 설비를 유지한 채 목록/로그 갱신 (행 색상은 DataBindingComplete에서 적용)
                    await LoadEquipmentListAsync();
                }
                catch (Exception ex)
                {
                    // 자동 갱신이라 오류 창은 띄우지 않음 (async void라 여기서 안 잡으면 전역 처리기의 오류 창이 뜸)
                    ReportBackgroundError("시뮬레이터 화면 갱신", ex);
                }
            }
        }

        #endregion

        #region DB 상태 표시

        // DB 연결 상태를 창 제목으로 표시하는 곳을 한 군데로 통일
        internal void SetDbStatus(bool ok)
        {
            if (IsDisposed)
            {
                return;
            }

            Text = ok ? FormTitle : $"{FormTitle} - DB 연결 확인 필요";
        }

        // 자동 작업(폴링, 시뮬레이터 화면 갱신)의 오류 처리: 오류 창 없이 기록만 하고, DB 문제면 창 제목으로 알림
        // (사용자가 직접 한 행동의 오류는 전역 처리기가 오류 창으로 알림)
        private void ReportBackgroundError(string source, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{source} 오류] {ex}");
            if (DbErrorHelper.IsDbError(ex))
            {
                SetDbStatus(false);
            }
        }

        #endregion
    }
}
