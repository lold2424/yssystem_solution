using yssystem_solution.Entities;
using yssystem_solution.Services;

namespace yssystem_solution
{
    public partial class Form1 : Form
    {
        private readonly EquipmentService _service = new EquipmentService();

        private bool _isBinding = false;

        public Form1()
        {
            InitializeComponent();

            Load += Form1_Load;
            btnAdd.Click += btnAdd_Click;
            btnChangeStatus.Click += btnChangeStatus_Click;
            dgvEquipment.SelectionChanged += dgvEquipment_SelectionChanged;
        }

        // 화면 초기화: 콤보박스/그리드 설정 후 설비 목록 조회
        private void Form1_Load(object? sender, EventArgs e)
        {
            cboLine.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLine.Items.AddRange(new object[] { "LINE_A", "LINE_B", "LINE_C" });
            cboLine.SelectedIndex = 0;

            cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStatus.Items.AddRange(new object[] { "IDLE", "RUN", "DOWN" });
            cboStatus.SelectedIndex = 0;

            InitGrid(dgvEquipment);
            InitGrid(dgvLogs);

            LoadEquipmentList();
        }

        // DataGridView 공통 설정 클래스 (읽기 전용, 한 행 전체 선택)
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

        // 설비 목록 조회 후 마스터 그리드에 바인딩 (selectEquipId가 있으면 해당 행을 다시 선택)
        private void LoadEquipmentList(string? selectEquipId = null)
        {
            try
            {
                _isBinding = true;
                dgvEquipment.DataSource = _service.GetEquipmentList();

                dgvEquipment.Columns["EquipId"]!.HeaderText = "설비ID";
                dgvEquipment.Columns["EquipName"]!.HeaderText = "설비명";
                dgvEquipment.Columns["LineName"]!.HeaderText = "라인";
                dgvEquipment.Columns["Status"]!.HeaderText = "상태";
                dgvEquipment.Columns["CreateDt"]!.HeaderText = "생성일시";

                if (selectEquipId != null)
                {
                    foreach (DataGridViewRow row in dgvEquipment.Rows)
                    {
                        if (row.Cells["EquipId"].Value as string == selectEquipId)
                        {
                            dgvEquipment.CurrentCell = row.Cells["EquipId"];
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설비 목록 조회 실패\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBinding = false;
            }

            // 선택된 설비의 로그도 함께 갱신
            LoadSelectedEquipmentLogs();
        }

        // 마스터 그리드에서 선택된 설비의 로그를 디테일 그리드에 바인딩
        private void LoadSelectedEquipmentLogs()
        {
            Equipment? selected = GetSelectedEquipment();
            if (selected == null)
            {
                dgvLogs.DataSource = null;
                return;
            }

            // 상태 콤보박스를 현재 설비 상태로 맞춤
            if (selected.Status != null && cboStatus.Items.Contains(selected.Status))
            {
                cboStatus.SelectedItem = selected.Status;
            }

            try
            {
                dgvLogs.DataSource = _service.GetLogsByEquipId(selected.EquipId);

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
            catch (Exception ex)
            {
                MessageBox.Show($"로그 조회 실패\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 마스터 그리드의 현재 선택 행을 Equipment로 반환
        private Equipment? GetSelectedEquipment()
        {
            return dgvEquipment.CurrentRow?.DataBoundItem as Equipment;
        }

        // 설비 선택 변경 시 로그 즉시 갱신
        private void dgvEquipment_SelectionChanged(object? sender, EventArgs e)
        {
            if (_isBinding)
            {
                return;
            }

            LoadSelectedEquipmentLogs();
        }

        // 신규 설비 등록
        private void btnAdd_Click(object? sender, EventArgs e)
        {
            string equipId = txtEquipId.Text.Trim();
            string equipName = txtEquipName.Text.Trim();
            string? lineName = cboLine.SelectedItem as string;

            if (equipId == "" || equipName == "" || lineName == null)
            {
                MessageBox.Show("설비ID, 설비명, 라인을 모두 입력하세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _service.AddEquipment(new Equipment
                {
                    EquipId = equipId,
                    EquipName = equipName,
                    LineName = lineName
                });

                txtEquipId.Clear();
                txtEquipName.Clear();

                // 등록한 설비를 선택한 상태로 목록 갱신
                LoadEquipmentList(equipId);
                MessageBox.Show("설비가 등록되었습니다.", "등록 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (InvalidOperationException ex)
            {
                // 중복 ID 등 업무 규칙 위반
                MessageBox.Show(ex.Message, "등록 불가", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설비 등록 실패\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 선택 설비 상태 변경 (UPDATE + 로그 INSERT 한 트랜잭션)
        private void btnChangeStatus_Click(object? sender, EventArgs e)
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

            try
            {
                _service.UpdateEquipmentStatus(selected.EquipId, newStatus);

                // 두 그리드 모두 갱신 (변경한 설비 선택 유지)
                LoadEquipmentList(selected.EquipId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상태 변경 실패\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}