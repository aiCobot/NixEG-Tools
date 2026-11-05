using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace LicenseGenerator
{
    public class MainForm : Form
    {
        private Label lblMachineCode, lblFeedbackCode, lblDays;
        private TextBox txtMachineCode, txtFeedbackCode, txtDays;
        private TextBox txtResult;
        private Button btnGenerate, btnCopy, btnClear;
        private ComboBox cmbDays;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "EVision 注册码生成器";
            this.Size = new Size(520, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Font = new Font("Microsoft YaHei UI", 10F);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // ---- 标题 ----
            var lblTitle = new Label
            {
                Text = "EVision 注册码生成器",
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 114),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(0, 10, 0, 0)
            };
            var sepTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = Color.FromArgb(30, 60, 114)
            };

            // ---- 输入面板 ----
            var panelInput = new Panel
            {
                Dock = DockStyle.Top,
                Height = 170,
                Padding = new Padding(30, 20, 30, 10)
            };

            lblMachineCode = new Label { Text = "机器码：", Location = new Point(0, 10), Size = new Size(80, 28) };
            txtMachineCode = new TextBox { Location = new Point(85, 8), Size = new Size(360, 28), Text = "ARPMNIZARMJHIDEB" };

            lblFeedbackCode = new Label { Text = "反馈码：", Location = new Point(0, 50), Size = new Size(80, 28) };
            txtFeedbackCode = new TextBox { Location = new Point(85, 48), Size = new Size(360, 28) };

            lblDays = new Label { Text = "天数：", Location = new Point(0, 90), Size = new Size(80, 28) };

            cmbDays = new ComboBox
            {
                Location = new Point(85, 88), Size = new Size(100, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbDays.Items.AddRange(new object[] { 1, 3, 5, 7, 15, 30, 60, 90, 180, 365 });
            cmbDays.SelectedIndex = 7; // 默认90天

            txtDays = new TextBox { Location = new Point(195, 88), Size = new Size(60, 28), ReadOnly = true };

            var lblOr = new Label { Text = "天", Location = new Point(262, 92), Size = new Size(20, 22), ForeColor = Color.Gray };

            cmbDays.SelectedIndexChanged += (s, e) =>
            {
                if (cmbDays.SelectedItem != null)
                    txtDays.Text = cmbDays.SelectedItem.ToString();
            };
            // 触发默认值
            txtDays.Text = "90";

            panelInput.Controls.AddRange(new Control[] {
                lblMachineCode, txtMachineCode,
                lblFeedbackCode, txtFeedbackCode,
                lblDays, cmbDays, txtDays, lblOr
            });

            // ---- 按钮面板 ----
            var panelBtn = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(30, 5, 30, 5)
            };

            btnGenerate = new Button
            {
                Text = "生 成 注 册 码",
                Location = new Point(0, 5),
                Size = new Size(170, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 60, 114),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerate.FlatAppearance.BorderSize = 0;

            btnCopy = new Button
            {
                Text = "复制注册码",
                Location = new Point(185, 5),
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10F),
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderSize = 0;

            btnClear = new Button
            {
                Text = "清空",
                Location = new Point(300, 5),
                Size = new Size(80, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 180, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10F),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;

            panelBtn.Controls.AddRange(new Control[] { btnGenerate, btnCopy, btnClear });

            // ---- 结果面板 ----
            var panelResult = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(30, 10, 30, 10)
            };

            var lblResultTitle = new Label
            {
                Text = "生成的注册码：",
                Location = new Point(0, 0),
                Size = new Size(120, 25),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };

            txtResult = new TextBox
            {
                Location = new Point(0, 28),
                Size = new Size(445, 32),
                Font = new Font("Consolas", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 0),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblNote = new Label
            {
                Text = "格式：XXXXXXXX-XXXXXXXX-XXXXXXXX-XXXXXXXX",
                Location = new Point(0, 65),
                Size = new Size(350, 20),
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei UI", 8F)
            };

            panelResult.Controls.AddRange(new Control[] { lblResultTitle, txtResult, lblNote });

            // ---- 底部状态栏 ----
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("就绪");
            statusStrip.Items.Add(statusLabel);

            // ---- 组装 ----
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            mainPanel.Controls.Add(panelResult);
            mainPanel.Controls.Add(panelBtn);
            mainPanel.Controls.Add(panelInput);

            this.Controls.Add(mainPanel);
            this.Controls.Add(sepTitle);
            this.Controls.Add(lblTitle);
            this.Controls.Add(statusStrip);

            // ---- 事件绑定 ----
            btnGenerate.Click += BtnGenerate_Click;
            btnCopy.Click += BtnCopy_Click;
            btnClear.Click += BtnClear_Click;

            this.AcceptButton = btnGenerate;

            this.Load += (s, e) => txtFeedbackCode.Focus();
        }

        private string GenerateLicense(string machineCode, string feedbackCode, int days)
        {
            string input = string.Format("JKVISION:{0}:{1}:{2}:{0}:JKVISION", days, machineCode, feedbackCode);
            byte[] data = Encoding.GetEncoding("iso-8859-1").GetBytes(input);
            byte[] hash;
            using (var md5 = MD5.Create())
            {
                hash = md5.ComputeHash(data);
            }
            string hex = BitConverter.ToString(hash).Replace("-", "").ToUpper();
            return string.Format("{0}-{1}-{2}-{3}",
                hex.Substring(0, 8), hex.Substring(8, 8),
                hex.Substring(16, 8), hex.Substring(24, 8));
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string machine = txtMachineCode.Text.Trim();
            string feedback = txtFeedbackCode.Text.Trim();
            string daysStr = txtDays.Text.Trim();

            if (string.IsNullOrEmpty(machine))
            {
                MessageBox.Show("请输入机器码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMachineCode.Focus();
                return;
            }
            if (string.IsNullOrEmpty(feedback))
            {
                MessageBox.Show("请输入反馈码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFeedbackCode.Focus();
                return;
            }
            int days;
            if (!int.TryParse(daysStr, out days) || days <= 0)
            {
                MessageBox.Show("请输入有效的天数", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbDays.Focus();
                return;
            }

            try
            {
                string license = GenerateLicense(machine, feedback, days);
                txtResult.Text = license;
                statusLabel.Text = string.Format("注册码生成成功（{0} 天）", days);
            }
            catch (Exception ex)
            {
                MessageBox.Show("生成失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "生成失败";
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtResult.Text))
            {
                Clipboard.SetText(txtResult.Text);
                statusLabel.Text = "已复制到剪贴板";
            }
            else
            {
                statusLabel.Text = "没有可复制的内容";
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtMachineCode.Text = "";
            txtFeedbackCode.Text = "";
            txtDays.Text = "";
            txtResult.Text = "";
            cmbDays.SelectedIndex = -1;
            statusLabel.Text = "已清空";
            txtFeedbackCode.Focus();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
