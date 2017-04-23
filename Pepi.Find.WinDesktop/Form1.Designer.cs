namespace Pepi.Find.WinDesktop
{
	partial class Form1
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
			if (disposing&&(components!=null))
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
			this.buttonStart = new System.Windows.Forms.Button();
			this.buttonStop = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxBindings = new System.Windows.Forms.TextBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.listViewMessages = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.textBoxRequest = new System.Windows.Forms.TextBox();
			this.textBoxResponse = new System.Windows.Forms.TextBox();
			this.textBoxConnectionString = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.buttonElevate = new System.Windows.Forms.Button();
			this.buttonTest = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonStart
			// 
			this.buttonStart.Location = new System.Drawing.Point(68, 64);
			this.buttonStart.Name = "buttonStart";
			this.buttonStart.Size = new System.Drawing.Size(75, 23);
			this.buttonStart.TabIndex = 0;
			this.buttonStart.Text = "Start";
			this.buttonStart.UseVisualStyleBackColor = true;
			this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
			// 
			// buttonStop
			// 
			this.buttonStop.Enabled = false;
			this.buttonStop.Location = new System.Drawing.Point(149, 64);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.Size = new System.Drawing.Size(75, 23);
			this.buttonStop.TabIndex = 1;
			this.buttonStop.Text = "Stop";
			this.buttonStop.UseVisualStyleBackColor = true;
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 41);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(50, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Bindings:";
			// 
			// textBoxBindings
			// 
			this.textBoxBindings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxBindings.Location = new System.Drawing.Point(68, 38);
			this.textBoxBindings.Name = "textBoxBindings";
			this.textBoxBindings.Size = new System.Drawing.Size(319, 20);
			this.textBoxBindings.TabIndex = 5;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.Location = new System.Drawing.Point(13, 93);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.listViewMessages);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
			this.splitContainer1.Size = new System.Drawing.Size(374, 280);
			this.splitContainer1.SplitterDistance = 123;
			this.splitContainer1.TabIndex = 6;
			// 
			// listViewMessages
			// 
			this.listViewMessages.AllowColumnReorder = true;
			this.listViewMessages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.listViewMessages.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listViewMessages.FullRowSelect = true;
			this.listViewMessages.GridLines = true;
			this.listViewMessages.Location = new System.Drawing.Point(0, 0);
			this.listViewMessages.Name = "listViewMessages";
			this.listViewMessages.ShowGroups = false;
			this.listViewMessages.Size = new System.Drawing.Size(123, 280);
			this.listViewMessages.TabIndex = 1;
			this.listViewMessages.UseCompatibleStateImageBehavior = false;
			this.listViewMessages.View = System.Windows.Forms.View.Details;
			this.listViewMessages.SelectedIndexChanged += new System.EventHandler(this.listViewMessages_SelectedIndexChanged);
			this.listViewMessages.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.listViewMessages_KeyPress);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Timestamp";
			this.columnHeader1.Width = 120;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Url";
			this.columnHeader2.Width = 350;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.textBoxRequest);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.textBoxResponse);
			this.splitContainer2.Size = new System.Drawing.Size(247, 280);
			this.splitContainer2.SplitterDistance = 140;
			this.splitContainer2.TabIndex = 0;
			// 
			// textBoxRequest
			// 
			this.textBoxRequest.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxRequest.Location = new System.Drawing.Point(0, 35);
			this.textBoxRequest.Multiline = true;
			this.textBoxRequest.Name = "textBoxRequest";
			this.textBoxRequest.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxRequest.Size = new System.Drawing.Size(247, 105);
			this.textBoxRequest.TabIndex = 0;
			// 
			// textBoxResponse
			// 
			this.textBoxResponse.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxResponse.Location = new System.Drawing.Point(0, 31);
			this.textBoxResponse.Multiline = true;
			this.textBoxResponse.Name = "textBoxResponse";
			this.textBoxResponse.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxResponse.Size = new System.Drawing.Size(247, 105);
			this.textBoxResponse.TabIndex = 1;
			// 
			// textBoxConnectionString
			// 
			this.textBoxConnectionString.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxConnectionString.Location = new System.Drawing.Point(68, 13);
			this.textBoxConnectionString.Name = "textBoxConnectionString";
			this.textBoxConnectionString.Size = new System.Drawing.Size(319, 20);
			this.textBoxConnectionString.TabIndex = 7;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(55, 13);
			this.label1.TabIndex = 8;
			this.label1.Text = "Conn. str.:";
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 1000;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// buttonElevate
			// 
			this.buttonElevate.Cursor = System.Windows.Forms.Cursors.IBeam;
			this.buttonElevate.Location = new System.Drawing.Point(231, 65);
			this.buttonElevate.Name = "buttonElevate";
			this.buttonElevate.Size = new System.Drawing.Size(75, 23);
			this.buttonElevate.TabIndex = 9;
			this.buttonElevate.Text = "Elevate";
			this.buttonElevate.UseVisualStyleBackColor = true;
			this.buttonElevate.Click += new System.EventHandler(this.buttonElevate_Click);
			// 
			// buttonTest
			// 
			this.buttonTest.Enabled = false;
			this.buttonTest.Location = new System.Drawing.Point(312, 65);
			this.buttonTest.Name = "buttonTest";
			this.buttonTest.Size = new System.Drawing.Size(75, 23);
			this.buttonTest.TabIndex = 10;
			this.buttonTest.Text = "Test";
			this.buttonTest.UseVisualStyleBackColor = true;
			this.buttonTest.Click += new System.EventHandler(this.buttonTest_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(399, 385);
			this.Controls.Add(this.buttonTest);
			this.Controls.Add(this.buttonElevate);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBoxConnectionString);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.textBoxBindings);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.buttonStop);
			this.Controls.Add(this.buttonStart);
			this.Name = "Form1";
			this.Text = "Pepi.Find";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel1.PerformLayout();
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonStart;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxBindings;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.TextBox textBoxConnectionString;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ListView listViewMessages;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.TextBox textBoxRequest;
		private System.Windows.Forms.TextBox textBoxResponse;
		private System.Windows.Forms.Button buttonElevate;
		private System.Windows.Forms.Button buttonTest;
	}
}

