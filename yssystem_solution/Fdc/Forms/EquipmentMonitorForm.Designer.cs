namespace yssystem_solution
{
    partial class EquipmentMonitorForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblEquipId = new Label();
            txtEquipId = new TextBox();
            lblEquipName = new Label();
            txtEquipName = new TextBox();
            lblLine = new Label();
            cboLine = new ComboBox();
            btnAdd = new Button();
            lblStatus = new Label();
            cboStatus = new ComboBox();
            btnChangeStatus = new Button();
            lblCheck = new Label();
            cboParam = new ComboBox();
            txtCheckVal = new TextBox();
            btnCheck = new Button();
            btnStart = new Button();
            btnStop = new Button();
            lblSimStatus = new Label();
            dgvEquipment = new DataGridView();
            dgvLogs = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)dgvEquipment).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvLogs).BeginInit();
            SuspendLayout();
            //
            // lblEquipId
            //
            lblEquipId.AutoSize = true;
            lblEquipId.Location = new Point(12, 19);
            lblEquipId.Name = "lblEquipId";
            lblEquipId.Size = new Size(43, 15);
            lblEquipId.TabIndex = 0;
            lblEquipId.Text = "설비ID";
            //
            // txtEquipId
            //
            txtEquipId.Location = new Point(72, 15);
            txtEquipId.Name = "txtEquipId";
            txtEquipId.Size = new Size(110, 23);
            txtEquipId.TabIndex = 1;
            //
            // lblEquipName
            //
            lblEquipName.AutoSize = true;
            lblEquipName.Location = new Point(200, 19);
            lblEquipName.Name = "lblEquipName";
            lblEquipName.Size = new Size(43, 15);
            lblEquipName.TabIndex = 2;
            lblEquipName.Text = "설비명";
            //
            // txtEquipName
            //
            txtEquipName.Location = new Point(256, 15);
            txtEquipName.Name = "txtEquipName";
            txtEquipName.Size = new Size(130, 23);
            txtEquipName.TabIndex = 3;
            //
            // lblLine
            //
            lblLine.AutoSize = true;
            lblLine.Location = new Point(404, 19);
            lblLine.Name = "lblLine";
            lblLine.Size = new Size(31, 15);
            lblLine.TabIndex = 4;
            lblLine.Text = "라인";
            //
            // cboLine
            //
            cboLine.FormattingEnabled = true;
            cboLine.Location = new Point(448, 15);
            cboLine.Name = "cboLine";
            cboLine.Size = new Size(110, 23);
            cboLine.TabIndex = 5;
            //
            // btnAdd
            //
            btnAdd.Location = new Point(576, 14);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(90, 25);
            btnAdd.TabIndex = 6;
            btnAdd.Text = "등록";
            btnAdd.UseVisualStyleBackColor = true;
            //
            // lblStatus
            //
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(12, 56);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(31, 15);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "상태";
            //
            // cboStatus
            //
            cboStatus.FormattingEnabled = true;
            cboStatus.Location = new Point(72, 52);
            cboStatus.Name = "cboStatus";
            cboStatus.Size = new Size(110, 23);
            cboStatus.TabIndex = 8;
            //
            // btnChangeStatus
            //
            btnChangeStatus.Location = new Point(196, 51);
            btnChangeStatus.Name = "btnChangeStatus";
            btnChangeStatus.Size = new Size(90, 25);
            btnChangeStatus.TabIndex = 9;
            btnChangeStatus.Text = "상태변경";
            btnChangeStatus.UseVisualStyleBackColor = true;
            //
            // lblCheck
            //
            lblCheck.AutoSize = true;
            lblCheck.Location = new Point(312, 56);
            lblCheck.Name = "lblCheck";
            lblCheck.Size = new Size(58, 15);
            lblCheck.TabIndex = 10;
            lblCheck.Text = "FDC 판정";
            //
            // cboParam
            //
            cboParam.FormattingEnabled = true;
            cboParam.Location = new Point(380, 52);
            cboParam.Name = "cboParam";
            cboParam.Size = new Size(80, 23);
            cboParam.TabIndex = 11;
            //
            // txtCheckVal
            //
            txtCheckVal.Location = new Point(470, 52);
            txtCheckVal.Name = "txtCheckVal";
            txtCheckVal.PlaceholderText = "측정값";
            txtCheckVal.Size = new Size(88, 23);
            txtCheckVal.TabIndex = 12;
            //
            // btnCheck
            //
            btnCheck.Location = new Point(576, 51);
            btnCheck.Name = "btnCheck";
            btnCheck.Size = new Size(90, 25);
            btnCheck.TabIndex = 13;
            btnCheck.Text = "판정";
            btnCheck.UseVisualStyleBackColor = true;
            //
            // btnStart
            //
            btnStart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStart.Location = new Point(724, 14);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(125, 25);
            btnStart.TabIndex = 14;
            btnStart.Text = "시뮬레이터 시작";
            btnStart.UseVisualStyleBackColor = true;
            //
            // btnStop
            //
            btnStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStop.Location = new Point(859, 14);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(125, 25);
            btnStop.TabIndex = 15;
            btnStop.Text = "시뮬레이터 정지";
            btnStop.UseVisualStyleBackColor = true;
            //
            // lblSimStatus
            //
            lblSimStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSimStatus.AutoEllipsis = true;
            lblSimStatus.Location = new Point(684, 56);
            lblSimStatus.Name = "lblSimStatus";
            lblSimStatus.Size = new Size(300, 15);
            lblSimStatus.TabIndex = 16;
            lblSimStatus.Text = "시뮬레이터: 정지";
            lblSimStatus.TextAlign = ContentAlignment.MiddleRight;
            //
            // dgvEquipment
            //
            dgvEquipment.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            dgvEquipment.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvEquipment.Location = new Point(12, 92);
            dgvEquipment.Name = "dgvEquipment";
            dgvEquipment.Size = new Size(560, 457);
            dgvEquipment.TabIndex = 17;
            //
            // dgvLogs
            //
            dgvLogs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLogs.Location = new Point(584, 92);
            dgvLogs.Name = "dgvLogs";
            dgvLogs.Size = new Size(400, 457);
            dgvLogs.TabIndex = 18;
            //
            // EquipmentMonitorForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(996, 561);
            Controls.Add(dgvLogs);
            Controls.Add(dgvEquipment);
            Controls.Add(lblSimStatus);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Controls.Add(btnCheck);
            Controls.Add(txtCheckVal);
            Controls.Add(cboParam);
            Controls.Add(lblCheck);
            Controls.Add(btnChangeStatus);
            Controls.Add(cboStatus);
            Controls.Add(lblStatus);
            Controls.Add(btnAdd);
            Controls.Add(cboLine);
            Controls.Add(lblLine);
            Controls.Add(txtEquipName);
            Controls.Add(lblEquipName);
            Controls.Add(txtEquipId);
            Controls.Add(lblEquipId);
            MinimumSize = new Size(1012, 400);
            Name = "EquipmentMonitorForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "설비 FDC 모니터링";
            ((System.ComponentModel.ISupportInitialize)dgvEquipment).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvLogs).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblEquipId;
        private TextBox txtEquipId;
        private Label lblEquipName;
        private TextBox txtEquipName;
        private Label lblLine;
        private ComboBox cboLine;
        private Button btnAdd;
        private Label lblStatus;
        private ComboBox cboStatus;
        private Button btnChangeStatus;
        private Label lblCheck;
        private ComboBox cboParam;
        private TextBox txtCheckVal;
        private Button btnCheck;
        private Button btnStart;
        private Button btnStop;
        private Label lblSimStatus;
        private DataGridView dgvEquipment;
        private DataGridView dgvLogs;
    }
}
