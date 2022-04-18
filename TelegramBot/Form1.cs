using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TradingFramework.TelegramBot;
using TradingFramework.ObserversFactory;
using System.Threading;
namespace TelegramBot
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        TfTelegramBot bot;
        private void button1_Click(object sender, EventArgs e)
        {
            bot = new TfTelegramBot(panel1);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bot != null)
                bot.Dispose();
        }
    }
}
