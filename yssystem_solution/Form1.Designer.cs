namespace yssystem_solution
{
    partial class Form1
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
            txtEquipId = new TextBox();
            txtEquipName = new TextBox();
            cboStatus = new ComboBox();
            cboLine = new ComboBox();
            btnChangeStatus = new Button();
            btnAdd = new Button();
            dgvEquipment = new DataGridView();
            dgvLogs = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)dgvEquipment).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvLogs).BeginInit();
            SuspendLayout();
            // 
            // txtEquipId
            // 
            txtEquipId.Location = new Point(41, 13);
            txtEquipId.Name = "txtEquipId";
            txtEquipId.Size = new Size(100, 23);
            txtEquipId.TabIndex = 0;
            // 
            // txtEquipName
            // 
            txtEquipName.Location = new Point(242, 13);
            txtEquipName.Name = "txtEquipName";
            txtEquipName.Size = new Size(100, 23);
            txtEquipName.TabIndex = 1;
            // 
            // cboStatus
            // 
            cboStatus.FormattingEnabled = true;
            cboStatus.Location = new Point(41, 66);
            cboStatus.Name = "cboStatus";
            cboStatus.Size = new Size(121, 23);
            cboStatus.TabIndex = 2;
            // 
            // cboLine
            // 
            cboLine.FormattingEnabled = true;
            cboLine.Location = new Point(410, 13);
            cboLine.Name = "cboLine";
            cboLine.Size = new Size(121, 23);
            cboLine.TabIndex = 3;
            // 
            // btnChangeStatus
            // 
            btnChangeStatus.Location = new Point(267, 66);
            btnChangeStatus.Name = "btnChangeStatus";
            btnChangeStatus.Size = new Size(75, 23);
            btnChangeStatus.TabIndex = 4;
            btnChangeStatus.Text = "상태변경";
            btnChangeStatus.UseVisualStyleBackColor = true;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(601, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(75, 23);
            btnAdd.TabIndex = 5;
            btnAdd.Text = "등록";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // dgvEquipment
            // 
            dgvEquipment.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvEquipment.Location = new Point(12, 104);
            dgvEquipment.Name = "dgvEquipment";
            dgvEquipment.Size = new Size(458, 334);
            dgvEquipment.TabIndex = 6;
            // 
            // dgvLogs
            // 
            dgvLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLogs.Location = new Point(491, 104);
            dgvLogs.Name = "dgvLogs";
            dgvLogs.Size = new Size(297, 334);
            dgvLogs.TabIndex = 7;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(dgvLogs);
            Controls.Add(dgvEquipment);
            Controls.Add(btnAdd);
            Controls.Add(btnChangeStatus);
            Controls.Add(cboLine);
            Controls.Add(cboStatus);
            Controls.Add(txtEquipName);
            Controls.Add(txtEquipId);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)dgvEquipment).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvLogs).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtEquipId;
        private TextBox txtEquipName;
        private ComboBox cboStatus;
        private ComboBox cboLine;
        private Button btnChangeStatus;
        private Button btnAdd;
        private DataGridView dgvEquipment;
        private DataGridView dgvLogs;
    }
}
