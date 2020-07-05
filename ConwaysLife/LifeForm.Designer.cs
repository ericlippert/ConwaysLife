namespace ConwaysLife
{
    partial class LifeForm
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
            this.components = new System.ComponentModel.Container();
            this.display = new System.Windows.Forms.PictureBox();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.panel = new System.Windows.Forms.Panel();
            this.speedLabel = new System.Windows.Forms.Label();
            this.slowerButton = new System.Windows.Forms.Label();
            this.fasterButton = new System.Windows.Forms.Label();
            this.loadButton = new System.Windows.Forms.Label();
            this.resetButton = new System.Windows.Forms.Label();
            this.playButton = new System.Windows.Forms.Label();
            this.title = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.display)).BeginInit();
            this.panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // display
            // 
            this.display.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.display.Location = new System.Drawing.Point(12, 12);
            this.display.Name = "display";
            this.display.Size = new System.Drawing.Size(560, 373);
            this.display.TabIndex = 0;
            this.display.TabStop = false;
            this.display.MouseDown += new System.Windows.Forms.MouseEventHandler(this.display_MouseDown);
            this.display.MouseEnter += new System.EventHandler(this.display_MouseEnter);
            this.display.MouseMove += new System.Windows.Forms.MouseEventHandler(this.display_MouseMove);
            this.display.MouseUp += new System.Windows.Forms.MouseEventHandler(this.display_MouseUp);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 30;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // panel
            // 
            this.panel.Controls.Add(this.textBox1);
            this.panel.Controls.Add(this.speedLabel);
            this.panel.Controls.Add(this.slowerButton);
            this.panel.Controls.Add(this.fasterButton);
            this.panel.Controls.Add(this.loadButton);
            this.panel.Controls.Add(this.resetButton);
            this.panel.Controls.Add(this.playButton);
            this.panel.Controls.Add(this.title);
            this.panel.Location = new System.Drawing.Point(12, 391);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(560, 70);
            this.panel.TabIndex = 1;
            // 
            // speedLabel
            // 
            this.speedLabel.Font = new System.Drawing.Font("Lucida Console", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.speedLabel.Location = new System.Drawing.Point(132, 27);
            this.speedLabel.Name = "speedLabel";
            this.speedLabel.Size = new System.Drawing.Size(45, 29);
            this.speedLabel.TabIndex = 7;
            this.speedLabel.Text = "15";
            this.speedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // slowerButton
            // 
            this.slowerButton.Font = new System.Drawing.Font("Lucida Console", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slowerButton.Location = new System.Drawing.Point(108, 25);
            this.slowerButton.Name = "slowerButton";
            this.slowerButton.Size = new System.Drawing.Size(29, 29);
            this.slowerButton.TabIndex = 6;
            this.slowerButton.Text = "⏬";
            this.slowerButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.slowerButton.Click += new System.EventHandler(this.slowerButton_Click);
            // 
            // fasterButton
            // 
            this.fasterButton.Font = new System.Drawing.Font("Lucida Console", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fasterButton.Location = new System.Drawing.Point(183, 25);
            this.fasterButton.Name = "fasterButton";
            this.fasterButton.Size = new System.Drawing.Size(29, 29);
            this.fasterButton.TabIndex = 5;
            this.fasterButton.Text = "⏫";
            this.fasterButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.fasterButton.Click += new System.EventHandler(this.fasterButton_Click);
            // 
            // loadButton
            // 
            this.loadButton.Font = new System.Drawing.Font("Lucida Console", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.loadButton.Location = new System.Drawing.Point(3, 25);
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(29, 29);
            this.loadButton.TabIndex = 4;
            this.loadButton.Text = "📁";
            this.loadButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.loadButton.Click += new System.EventHandler(this.loadButton_Click);
            // 
            // resetButton
            // 
            this.resetButton.Font = new System.Drawing.Font("Lucida Console", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.resetButton.Location = new System.Drawing.Point(38, 25);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(29, 29);
            this.resetButton.TabIndex = 3;
            this.resetButton.Text = "⏮︎";
            this.resetButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            // 
            // playButton
            // 
            this.playButton.Font = new System.Drawing.Font("Lucida Console", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.playButton.Location = new System.Drawing.Point(73, 25);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(29, 29);
            this.playButton.TabIndex = 2;
            this.playButton.Text = "⏯︎";
            this.playButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.playButton.Click += new System.EventHandler(this.playButton_Click);
            // 
            // title
            // 
            this.title.AutoSize = true;
            this.title.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.title.Location = new System.Drawing.Point(5, 0);
            this.title.Name = "title";
            this.title.Size = new System.Drawing.Size(168, 16);
            this.title.TabIndex = 0;
            this.title.Text = "Life is Fabulous";
            // 
            // textBox1
            // 
            this.textBox1.AcceptsReturn = true;
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(218, 3);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(222, 67);
            this.textBox1.TabIndex = 8;
            // 
            // LifeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 471);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.display);
            this.KeyPreview = true;
            this.Name = "LifeForm";
            this.Text = "LifeForm";
            this.Load += new System.EventHandler(this.LifeForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LifeForm_KeyDown);
            this.Resize += new System.EventHandler(this.LifeForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.display)).EndInit();
            this.panel.ResumeLayout(false);
            this.panel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox display;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.Label title;
        private System.Windows.Forms.Label playButton;
        private System.Windows.Forms.Label resetButton;
        private System.Windows.Forms.Label loadButton;
        private System.Windows.Forms.Label speedLabel;
        private System.Windows.Forms.Label slowerButton;
        private System.Windows.Forms.Label fasterButton;
        private System.Windows.Forms.TextBox textBox1;
    }
}

