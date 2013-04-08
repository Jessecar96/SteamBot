using System;
using System.Windows.Forms;

namespace SteamBot
{
    public partial class SteamGuardForm : Form
    {
        public SteamGuardForm(string botName)
        {
            InitializeComponent();

            lblBotName.Text = botName;
        }

        public string UserEnteredCode { get; private set; }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            var input = txtSteamGuard.Text;

            if (String.IsNullOrEmpty(input))
                return;

            input = input.Trim();

            UserEnteredCode = input;

            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
