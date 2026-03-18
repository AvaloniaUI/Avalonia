using Avalonia.Win32.Interoperability;

namespace WindowsInteropTest
{
    partial class EmbedToWinFormsDemo
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
            OpenWindowButton = new System.Windows.Forms.Button();
            monthCalendar1 = new System.Windows.Forms.MonthCalendar();
            groupBox1 = new System.Windows.Forms.GroupBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            avaloniaHost = new Avalonia.Win32.Interoperability.WinFormsAvaloniaControlHost();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            //
            // OpenWindowButton
            //
            OpenWindowButton.Location = new System.Drawing.Point(33, 33);
            OpenWindowButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            OpenWindowButton.Name = "OpenWindowButton";
            OpenWindowButton.Size = new System.Drawing.Size(191, 84);
            OpenWindowButton.TabIndex = 0;
            OpenWindowButton.Text = "Open Avalonia Window";
            OpenWindowButton.UseVisualStyleBackColor = true;
            OpenWindowButton.Click += OpenWindowButton_Click;
            //
            // monthCalendar1
            //
            monthCalendar1.Location = new System.Drawing.Point(33, 132);
            monthCalendar1.Margin = new System.Windows.Forms.Padding(10);
            monthCalendar1.Name = "monthCalendar1";
            monthCalendar1.TabIndex = 1;
            //
            // groupBox1
            //
            groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left));
            groupBox1.Controls.Add(OpenWindowButton);
            groupBox1.Controls.Add(monthCalendar1);
            groupBox1.Location = new System.Drawing.Point(14, 14);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(265, 482);
            groupBox1.TabIndex = 2;
            groupBox1.TabStop = false;
            groupBox1.Text = "WinForms";
            //
            // groupBox2
            //
            groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
            groupBox2.Controls.Add(avaloniaHost);
            groupBox2.Location = new System.Drawing.Point(286, 14);
            groupBox2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox2.Size = new System.Drawing.Size(584, 482);
            groupBox2.TabIndex = 3;
            groupBox2.TabStop = false;
            groupBox2.Text = "Avalonia";
            //
            // avaloniaHost
            //
            avaloniaHost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
            avaloniaHost.Location = new System.Drawing.Point(7, 22);
            avaloniaHost.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            avaloniaHost.Name = "avaloniaHost";
            avaloniaHost.Size = new System.Drawing.Size(570, 453);
            avaloniaHost.TabIndex = 0;
            avaloniaHost.Text = "avaloniaHost";
            //
            // EmbedToWinFormsDemo
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(884, 510);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MinimumSize = new System.Drawing.Size(697, 456);
            Text = "EmbedToWinFormsDemo";
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button OpenWindowButton;
        private System.Windows.Forms.MonthCalendar monthCalendar1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private WinFormsAvaloniaControlHost avaloniaHost;
    }
}
