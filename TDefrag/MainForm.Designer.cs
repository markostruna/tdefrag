namespace TDefrag
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.diskArray = new System.Windows.Forms.ComboBox();
            this.startDefrag = new System.Windows.Forms.Button();
            this.defragLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // diskArray
            // 
            this.diskArray.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.diskArray.FormattingEnabled = true;
            this.diskArray.Location = new System.Drawing.Point(4, 3);
            this.diskArray.Name = "diskArray";
            this.diskArray.Size = new System.Drawing.Size(121, 21);
            this.diskArray.TabIndex = 0;
            // 
            // startDefrag
            // 
            this.startDefrag.Location = new System.Drawing.Point(131, 1);
            this.startDefrag.Name = "startDefrag";
            this.startDefrag.Size = new System.Drawing.Size(75, 23);
            this.startDefrag.TabIndex = 1;
            this.startDefrag.Text = "Defrag";
            this.startDefrag.UseVisualStyleBackColor = true;
            this.startDefrag.Click += new System.EventHandler(this.startDefrag_Click);
            // 
            // defragLog
            // 
            this.defragLog.Location = new System.Drawing.Point(4, 30);
            this.defragLog.MaxLength = 327670;
            this.defragLog.Multiline = true;
            this.defragLog.Name = "defragLog";
            this.defragLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.defragLog.Size = new System.Drawing.Size(560, 425);
            this.defragLog.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(573, 464);
            this.Controls.Add(this.defragLog);
            this.Controls.Add(this.startDefrag);
            this.Controls.Add(this.diskArray);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox diskArray;
        private System.Windows.Forms.Button startDefrag;
        private System.Windows.Forms.TextBox defragLog;
    }
}

