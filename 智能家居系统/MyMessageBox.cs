﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 智能家居系统
{
    public partial class MyMessageBox : Form
    {
        /// <summary>
        /// 图标类型枚举
        /// </summary>
        public enum IconType
        {
            /// <summary>
            /// 提示信息
            /// </summary>
            Info,
            /// <summary>
            /// 询问
            /// </summary>
            Question,
            /// <summary>
            /// 警告
            /// </summary>
            Warning,
            /// <summary>
            /// 错误
            /// </summary>
            Error
        }

        #region "构造函数"
        private MyMessageBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 弹出提示窗口
        /// </summary>
        /// <param name="MessageText">消息文本</param>
        /// <param name="iconType">图表类型</param>
        public MyMessageBox(string MessageText, IconType iconType) : this(MessageText, "智能家居系统：", iconType){ }

        /// <summary>
        /// 弹出提示窗口
        /// </summary>
        /// <param name="MessageText">消息文本</param>
        /// <param name="Title">消息标题</param>
        /// <param name="iconType">图表类型</param>
        public MyMessageBox(string MessageText,string Title,IconType iconType)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            TitleLabel.Text = Title;
            MessageLabel.Text = MessageText;
            IconLabel.Image = UnityResource.ResourceManager.GetObject(iconType.ToString()) as Image;
            if (iconType == IconType.Question)
            {
                CancelButton.Left = (this.Width - CancelButton.Width * 2 - 20)/2;
                OKButton.Left = CancelButton.Right + 20;
            }
            else
            {
                CancelButton.Hide();
                OKButton.Left =(this.Width-OKButton.Width)/2;
            }
        }
        #endregion

        #region "按钮动态效果"
        private void Button_MouseDown(object sender, MouseEventArgs e)
        {
            (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_2") as Image;
        }

        private void Button_MouseEnter(object sender, EventArgs e)
        {
            (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_1") as Image;
        }

        private void Button_MouseLeave(object sender, EventArgs e)
        {
            (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_0") as Image;
        }

        private void Button_MouseUp(object sender, MouseEventArgs e)
        {
            (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_1") as Image;
        }
        #endregion

        #region "按钮点击事件"
        private void CloseButton_Click(object sender, EventArgs e)
        {
            HideMe(DialogResult.Cancel);
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            HideMe(DialogResult.OK);
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            HideMe(DialogResult.Cancel);
        }
        #endregion

        #region "窗体事件"
        private void MyMessageBox_Load(object sender, EventArgs e)
        {
            this.Icon = UnityResource.LogoIcon;
            //注册鼠标拖动功能
            TitleLabel.MouseDown += new MouseEventHandler(UnityModule.MoveFormViaMouse);

            CloseButton.MouseEnter += new EventHandler(Button_MouseEnter);
            CloseButton.MouseLeave += new EventHandler(Button_MouseLeave);
            CloseButton.MouseDown += new MouseEventHandler(Button_MouseDown);
            CloseButton.MouseUp += new MouseEventHandler(Button_MouseUp);

            OKButton.MouseEnter += new EventHandler(Button_MouseEnter);
            OKButton.MouseLeave += new EventHandler(Button_MouseLeave);
            OKButton.MouseDown += new MouseEventHandler(Button_MouseDown);
            OKButton.MouseUp += new MouseEventHandler(Button_MouseUp);

            CancelButton.MouseEnter += new EventHandler(Button_MouseEnter);
            CancelButton.MouseLeave += new EventHandler(Button_MouseLeave);
            CancelButton.MouseDown += new MouseEventHandler(Button_MouseDown);
            CancelButton.MouseUp += new MouseEventHandler(Button_MouseUp);
        }

        private void MyMessageBox_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None)
            {
                e.Cancel = true;
                HideMe(DialogResult.Cancel);
            }
        }

        private void MyMessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                HideMe(DialogResult.Cancel);
            else if (e.KeyCode == Keys.Enter)
                HideMe(DialogResult.OK);
        }
        #endregion

        #region "功能函数"
        /// <summary>
        /// 动态隐藏窗体并返回对话框DialogResult
        /// </summary>
        /// <param name="TargetDialogResult">对话框返回值</param>
        private void HideMe(DialogResult TargetDialogResult)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate{
                while (Opacity > 0)
                {
                    Opacity -= 0.1;
                    Thread.Sleep(10);
                }
                this.DialogResult = (DialogResult)TargetDialogResult;
            }));
        }

        /// <summary>
        /// 弹出需要用户输入内容的 InputBox
        /// </summary>
        /// <param name="InputBoxTips">输入内容的提示信息</param>
        /// <param name="UserInput">用于接收数据的字符串对象（ref 传址）</param>
        /// <param name="DefaultString">默认字符串</param>
        /// <param name="MaxLength">最大文本长度</param>
        /// <returns>输入框返回值（判断用户是否取消了输入）</returns>
        static public DialogResult ShowInputBox(string InputBoxTips,ref string UserInput,string DefaultString,int MaxLength)
        {
            MyMessageBox InputBoxForm = new MyMessageBox(InputBoxTips,"请输入信息：",IconType.Question);
            InputBoxForm.MessageLabel.Height = 70;
            InputBoxForm.InputTextBox.Show();
            InputBoxForm.InputTextBox.Text = DefaultString;
            InputBoxForm.InputTextBox.MaxLength = MaxLength;
            InputBoxForm.InputTextBox.KeyDown += new KeyEventHandler(delegate(object x,KeyEventArgs y) {
                if (y.KeyCode == Keys.Escape)
                    InputBoxForm.CancelButton_Click(InputBoxForm.CancelButton,new EventArgs());
                else if (y.KeyCode == Keys.Enter)
                    InputBoxForm.OKButton_Click(InputBoxForm.OKButton, new EventArgs());
            });
            DialogResult InputBoxDialogResult = InputBoxForm.ShowDialog();
            if (InputBoxDialogResult == DialogResult.OK)
            {
                UserInput = InputBoxForm.InputTextBox.Text;
            }
            return InputBoxDialogResult;
        }
        #endregion

    }
}
