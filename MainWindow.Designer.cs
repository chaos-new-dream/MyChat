


namespace MyChat
{
	partial class MainWindow
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
			components = new System.ComponentModel.Container();
			textBox1 = new TextBox();
			textBox2 = new TextBox();
			timer1 = new System.Windows.Forms.Timer(components);
			SuspendLayout();
			// 
			// textBox1
			// 
			textBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			textBox1.Font = new Font("宋体", 16F, FontStyle.Regular, GraphicsUnit.Point);
			textBox1.Location = new Point(12, 12);
			textBox1.Name = "textBox1";
			textBox1.Size = new Size(312, 32);
			textBox1.TabIndex = 0;
			textBox1.TextChanged += textBox1_TextChanged;
			textBox1.KeyPress += textBox1_KeyPress;
			// 
			// textBox2
			// 
			textBox2.AcceptsReturn = true;
			textBox2.AcceptsTab = true;
			textBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			textBox2.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			textBox2.Location = new Point(12, 53);
			textBox2.Multiline = true;
			textBox2.Name = "textBox2";
			textBox2.ReadOnly = true;
			textBox2.ScrollBars = ScrollBars.Vertical;
			textBox2.Size = new Size(312, 385);
			textBox2.TabIndex = 1;
			textBox2.TabStop = false;
			textBox2.Text = "这是聊天的历史记录";
			// 
			// timer1
			// 
			timer1.Tick += timer1_Tick;
			// 
			// MainWindow
			// 
			AutoScaleDimensions = new SizeF(7F, 17F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(336, 450);
			Controls.Add(textBox2);
			Controls.Add(textBox1);
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "MainWindow";
			Opacity = 0.8D;
			Text = "聊天室";
			TopMost = true;
			FormClosing += MainWindow_FormClosing;
			Load += Form1_Load;
			MouseDown += MainWindow_MouseDown;
			MouseMove += MainWindow_MouseMove;
			MouseUp += MainWindow_MouseUp;
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private TextBox textBox1;
		private TextBox textBox2;
		private System.Windows.Forms.Timer timer1;
	}
}
