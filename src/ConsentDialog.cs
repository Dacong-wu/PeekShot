using System;
using System.Drawing;
using System.Windows.Forms;

namespace PeekShot
{
    /// <summary>
    /// 同意弹窗：分步展示 监听 → 拍照 → 锁屏 流程，
    /// 键帽样式展示退出热键，隐私提示单独成块。
    /// “是”= 启用监控；“否”、Esc 或关闭窗口 = 退出。
    /// </summary>
    internal class ConsentDialog : Form
    {
        private const int ClientWidth = 540;
        private const int ClientHeight = 472;

        private static readonly Color Accent = Color.FromArgb(0, 120, 215);
        private static readonly Color TextPrimary = Color.FromArgb(32, 33, 36);
        private static readonly Color TextSecondary = Color.FromArgb(100, 104, 108);

        public ConsentDialog(Config config)
        {
            Text = "PeekShot";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Microsoft YaHei UI", 9.5F);
            BackColor = Color.White;
            ClientSize = new Size(ClientWidth, ClientHeight);

            // ---------- 头部 ----------
            PictureBox icon = new PictureBox();
            icon.Image = SystemIcons.Shield.ToBitmap();
            icon.Size = new Size(40, 40);
            icon.Location = new Point(28, 24);
            icon.SizeMode = PictureBoxSizeMode.CenterImage;
            Controls.Add(icon);

            Label title = new Label();
            title.Text = "启用摄像头保护？";
            title.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            title.ForeColor = TextPrimary;
            title.AutoSize = true;
            title.Location = new Point(80, 26);
            Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "离开电脑后若有人碰了键鼠，会自动拍照并锁定电脑";
            subtitle.ForeColor = TextSecondary;
            subtitle.AutoSize = true;
            subtitle.Location = new Point(82, 60);
            Controls.Add(subtitle);

            Controls.Add(MakeSeparator(92));

            // ---------- 流程步骤 ----------
            AddStep(1, "启动后后台监听键盘和鼠标", 110);
            AddStep(2, "首次检测到输入：摄像头拍照保存到 图片\\PeekShot（仅本机）", 152);
            AddStep(3, "随后自动锁定屏幕", 194);

            // ---------- 退出热键 ----------
            Label hotkeyCaption = new Label();
            hotkeyCaption.Text = "本人返回时按以下热键可正常退出（不拍照、不锁屏）：";
            hotkeyCaption.ForeColor = TextSecondary;
            hotkeyCaption.AutoSize = true;
            hotkeyCaption.Location = new Point(28, 244);
            Controls.Add(hotkeyCaption);

            BuildHotkeyRow(config.HotkeyText, 270);

            // ---------- 隐私提示 ----------
            Panel warning = new Panel();
            warning.Size = new Size(ClientWidth - 56, 78);
            warning.Location = new Point(28, 318);
            warning.BackColor = Color.FromArgb(255, 249, 235);
            warning.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Color.FromArgb(245, 195, 110)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, warning.Width - 1, warning.Height - 1);
                }
            };

            Label warningText = new Label();
            warningText.Text = "请仅在你有权使用摄像头、且符合相关人员隐私要求的情况下启用。" +
                "照片不会由本程序上传。";
            warningText.ForeColor = Color.FromArgb(120, 85, 20);
            warningText.Location = new Point(14, 10);
            warningText.Size = new Size(warning.Width - 28, warning.Height - 20);
            warning.Controls.Add(warningText);
            Controls.Add(warning);

            // ---------- 按钮 ----------
            Button noButton = new Button();
            noButton.Text = "否，退出";
            noButton.Size = new Size(116, 36);
            noButton.Location = new Point(ClientWidth - 28 - 132 - 12 - 116, ClientHeight - 36 - 24);
            noButton.DialogResult = DialogResult.No;
            noButton.FlatStyle = FlatStyle.Flat;
            noButton.ForeColor = TextPrimary;
            noButton.BackColor = Color.White;
            noButton.FlatAppearance.BorderColor = Color.FromArgb(200, 204, 208);
            noButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 246, 247);
            noButton.TabIndex = 1;
            Controls.Add(noButton);

            Button yesButton = new Button();
            yesButton.Text = "是，启用保护";
            yesButton.Size = new Size(132, 36);
            yesButton.Location = new Point(ClientWidth - 28 - 132, ClientHeight - 36 - 24);
            yesButton.DialogResult = DialogResult.Yes;
            yesButton.FlatStyle = FlatStyle.Flat;
            yesButton.ForeColor = Color.White;
            yesButton.BackColor = Accent;
            yesButton.FlatAppearance.BorderSize = 0;
            yesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 141, 255);
            yesButton.TabIndex = 0;
            yesButton.Select();
            Controls.Add(yesButton);

            AcceptButton = yesButton;
            CancelButton = noButton;
        }

        private static Label MakeSeparator(int y)
        {
            Label separator = new Label();
            separator.BackColor = Color.FromArgb(232, 234, 237);
            separator.Location = new Point(28, y);
            separator.Size = new Size(ClientWidth - 56, 1);
            return separator;
        }

        private void AddStep(int number, string text, int y)
        {
            Panel circle = new Panel();
            circle.Size = new Size(26, 26);
            circle.Location = new Point(28, y);
            circle.Tag = number.ToString();
            circle.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(Accent))
                {
                    e.Graphics.FillEllipse(brush, 1, 1, 24, 24);
                }
                TextRenderer.DrawText(e.Graphics, (string)circle.Tag, Font,
                    new Rectangle(0, 0, 26, 26), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            Controls.Add(circle);

            Label label = new Label();
            label.Text = text;
            label.ForeColor = TextPrimary;
            label.AutoSize = false;
            label.Size = new Size(ClientWidth - 56 - 40, 30);
            label.Location = new Point(66, y + 1);
            Controls.Add(label);
        }

        private void BuildHotkeyRow(string hotkeyText, int y)
        {
            if (string.IsNullOrEmpty(hotkeyText))
            {
                Label disabled = new Label();
                disabled.Text = "（已禁用退出热键 — 请通过 PeekShot.ini 配置）";
                disabled.ForeColor = Color.FromArgb(200, 60, 40);
                disabled.AutoSize = true;
                disabled.Location = new Point(30, y + 5);
                Controls.Add(disabled);
                return;
            }

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Location = new Point(28, y);
            row.Size = new Size(ClientWidth - 56, 34);
            row.BackColor = Color.White;
            row.WrapContents = false;

            string[] keys = hotkeyText.Split('+');
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i].Trim();
                if (key.Length == 0) continue;
                if (i > 0)
                {
                    Label plus = new Label();
                    plus.Text = "+";
                    plus.ForeColor = TextSecondary;
                    plus.AutoSize = true;
                    plus.Margin = new Padding(4, 8, 4, 0);
                    row.Controls.Add(plus);
                }
                row.Controls.Add(MakeKeycap(key));
            }

            Label hint = new Label();
            hint.Text = "即可解锁进入";
            hint.ForeColor = TextSecondary;
            hint.AutoSize = true;
            hint.Margin = new Padding(10, 8, 0, 0);
            row.Controls.Add(hint);

            Controls.Add(row);
        }

        private static Label MakeKeycap(string key)
        {
            Label keycap = new Label();
            keycap.Text = key;
            keycap.AutoSize = true;
            keycap.BackColor = Color.FromArgb(248, 249, 250);
            keycap.BorderStyle = BorderStyle.FixedSingle;
            keycap.Padding = new Padding(10, 5, 10, 5);
            keycap.Margin = new Padding(0, 2, 0, 0);
            keycap.Font = new Font("Consolas", 10F, FontStyle.Bold);
            keycap.ForeColor = TextPrimary;
            return keycap;
        }
    }
}
