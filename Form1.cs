using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MySql.Data.MySqlClient;

namespace ComPortSerial
{
    public partial class Form1 : Form
    {
        string sendWith;
        string dataIN;
        int dataINLength;
        int[] dataInDec;

        StreamWriter objStreamWriter;
        //string pathFile = @"C:\Users\admin\source\repos\ComPort\_My Source File\SerialData.txt";
        string pathFile;

        bool state_AppendText = true;

        MySqlConnection myConnection;
        MySqlCommand myCommand;

        #region My Own Method
        private void SaveDataToTxtFile()
        {
            if (saveToTexToolStripMenuItem.Checked)
            {
                //텍스트 파일에 저장
                try
                {
                    objStreamWriter = new StreamWriter(pathFile, state_AppendText);
                    if (toolStripComboBox_writeLineOrwriteText.Text == "WriteLine")
                        objStreamWriter.WriteLine(dataIN);
                    else if (toolStripComboBox_writeLineOrwriteText.Text == "Write")
                        objStreamWriter.Write(dataIN + " ");

                    objStreamWriter.Close();
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }

        private void SaveDataToMySqlDatabase()
        {
            if (saveToMySQLDatabaseToolStripMenuItem.Checked)
            {
                try
                {
                    myConnection = new MySqlConnection("server=localhost; username=root; password=1234; port=3306; database=database01");
                    myConnection.Open();
                    myCommand = new MySqlCommand(string.Format("INSERT INTO `table01` VALUES('{0}')", dataIN), myConnection);
                    myCommand.ExecuteNonQuery();

                    myConnection.Close();

                    RefreshDataGridViewForm2();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }
        #region Custom EventHandler
        public delegate void UpdateDelegate(object sender, UpdateDataEventArgs args);

        public event UpdateDelegate UpdateDataEventHandler;
        public class UpdateDataEventArgs : EventArgs
        {

        }
        protected void RefreshDataGridViewForm2()
        {
            UpdateDataEventArgs args = new UpdateDataEventArgs();
            UpdateDataEventHandler.Invoke(this, args);
        }
        #endregion

        #region RX Data Format
        private String RxDataFormat(int[] dataInput)
        {
            string strOut = "";
            if (toolStripComboBox_RxDataFormat.Text == "Hex")
            {
                foreach(int element in dataInput)
                {
                    strOut += Convert.ToString(element, 16) + "\t";
                }
            }
            else if (toolStripComboBox_RxDataFormat.Text == "Decimal")
            {
                foreach (int element in dataInput)
                {
                    strOut += Convert.ToString(element) + "\t";
                }
            }
            else if (toolStripComboBox_RxDataFormat.Text == "Binary")
            {
                foreach (int element in dataInput)
                {
                    strOut += Convert.ToString(element, 2) + "\t";
                }
            }
            else if (toolStripComboBox_RxDataFormat.Text == "Char")
            {
                foreach (int element in dataInput)
                {
                    strOut += Convert.ToChar(element);
                }
            }
            return strOut;
        }

        #endregion

        #region Tx Data Format
        private void TxDataFormat()
        {
            if (toolStripComboBox_TxDataFormat.Text == "Char")
            {
                //텍스트 박스의 데이터를 직렬 포트를 통해 전송
                serialPort1.Write(tBoxDataOut.Text);

                // 글자수 체크
                int dataOUTLength = tBoxDataOut.TextLength;
                lblDataOutLength.Text = string.Format("{0:00}", dataOUTLength);
            }
            else
            {
                //지역 변수 선언(Declare Local Variable)
                string dataOutBuffer;
                int countComma = 0;
                string[] dataPrepareToSend;
                byte[] dataToSend;
                try
                {
                    // 텍스트 박스에 있는 데이터 패키지를 변수로 이동
                    dataOutBuffer = tBoxDataOut.Text;

                    //데이터 패키지에서 쉼표 (,)의 개수 확인
                    foreach (char c in dataOutBuffer)
                    {
                        if (c == ',')
                            countComma++;
                    }

                    //쉼표 개수만큼의 길이를 가진 문자열 배열을 생성
                    dataPrepareToSend = new string[countComma];

                    //dataOutBuffer의 데이터를 쉼표를 기준으로 파싱하여 dataPrepareToSend 배열에 저장
                    countComma = 0;
                    foreach(char c in dataOutBuffer)
                    {
                        if((c != ','))
                        {
                            //데이터를 dataPrepareToSend 배열에 추가
                            dataPrepareToSend[countComma] += c;
                        }
                        else
                        {
                            //데이터 패키지에서 쉼표를 발견하면 countComma 변수를 증가
                            //countComma는 dataPrepareToSend 배열의 인덱스를 결정하는 데 사용
                            countComma++;
                            //countComma의 수가 dataPrepareToSend의 크기와 같으면 foreach 프로세스를 중지
                            if (countComma == dataPrepareToSend.GetLength(0))
                                break;
                        }
                    }

                    //dataPrepareToSend의 크기를 기준으로 길이가 설정된 바이트배열 생성
                    dataToSend = new byte[dataPrepareToSend.Length];

                    if (toolStripComboBox_TxDataFormat.Text == "Hex")
                    {
                        //문자열 배열 (dataPrepareToSend)의 데이터를 바이트 배열 (dataToSend)로 변환
                        for (int i = 0; i < dataPrepareToSend.Length; i++)
                        {
                            //문자열을 지정된 진수로 8비트 부호 없는 정수로 변환
                            //값 16은 16진수를 의미 - Value 16 means Hexa
                            dataToSend[i] = Convert.ToByte(dataPrepareToSend[i], 16);
                        }
                    }
                    else if (toolStripComboBox_TxDataFormat.Text == "Binary")
                    {
                        //문자열 배열 (dataPrepareToSend)의 데이터를 바이트 배열 (dataToSend)로 변환
                        for (int i = 0; i < dataPrepareToSend.Length; i++)
                        {
                            //문자열을 지정된 진수로 8비트 부호 없는 정수로 변환
                            //값 2는 이진수를 의미 - Value 2 mean Binary
                            dataToSend[i] = Convert.ToByte(dataPrepareToSend[i], 2);
                        }
                    }
                    else if (toolStripComboBox_TxDataFormat.Text == "Decimal")
                    {
                        //문자열 배열 (dataPrepareToSend)의 데이터를 바이트 배열 (dataToSend)로 변환
                        for (int i = 0; i < dataPrepareToSend.Length; i++)
                        {
                            //문자열을 지정된 진수로 8비트 부호 없는 정수로 변환
                            //값 10은 십진수를 의미 - Value 10 mean Decimal
                            dataToSend[i] = Convert.ToByte(dataPrepareToSend[i], 10);
                        }
                    }
                    //지정된 바이트 수를 직렬 포트로 전송
                    serialPort1.Write(dataToSend, 0, dataToSend.Length);

                    //전송된 데이터의 길이를 계산한 다음, 표시
                    lblDataOutLength.Text = string.Format("{0:00}", dataToSend.Length);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void TxSendData()
        {
            if (serialPort1.IsOpen)
            {
                //dataOUT = tBoxDataOut.Text;
                if (sendWith == "None")
                {
                    //serialPort1.Write(dataOUT);
                    TxDataFormat();
                }
                else if (sendWith == @"Both (\r\n)")
                {
                    //serialPort1.Write(dataOUT + "\r\n");
                    TxDataFormat();
                    serialPort1.Write("\r\n");
                }
                else if (sendWith == @"New Line (\n)")
                {
                    //serialPort1.Write(dataOUT + "\n");
                    TxDataFormat();
                    serialPort1.Write("\n");
                }
                else if (sendWith == @"Carriage Return (\r)")
                {
                    //serialPort1.Write(dataOUT + "\r");
                    TxDataFormat();
                    serialPort1.Write("\r");
                }
            }

            if (clearToolStripMenuItem.Checked)
            {
                if (tBoxDataOut.Text != "")
                    tBoxDataOut.Text = "";
            }
        }

        #endregion

        #endregion

        #region GUI Method
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //DtrEnable 체크 박스 기본값
            chBoxDtrEnable.Checked = false;
            serialPort1.DtrEnable = false;
            //RTSEnable 체크박스 기본값
            chBoxRTSEnable.Checked = false;
            serialPort1.RtsEnable = false;
            //Using Button 체크박스 기본값
            btnSendData.Enabled = true;

            sendWith = @"Both (\r\n)";
            toolStripComboBox_RxDataPosition.Text = "BOTTOM";

            toolStripComboBox_RxShowDataWith.Text = "Add to Old Data";
            toolStripComboBox_TxEndLine.Text = @"Both (\r\n)";

            toolStripComboBox_appendOrOverwriteText.Text = "Append Text";
            toolStripComboBox_writeLineOrwriteText.Text = "WriteLine";

            pathFile = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory()));
            pathFile += @"\_My Source File\SerialData.txt";

            //위치 확인
            //Console.WriteLine("======== Below is The Result =========");
            //Console.WriteLine(pathFile);
            //C:\Users\admin\source\repos\ComPort\_My Source File
            
            saveToTexToolStripMenuItem.Checked = false;
            saveToMySQLDatabaseToolStripMenuItem.Checked = false;

            toolStripComboBox_RxDataFormat.Text = "Char";
            toolStripComboBox_TxDataFormat.Text = "Char";

            this.toolStripComboBox_TxDataFormat.ComboBox.SelectionChangeCommitted += new System.EventHandler(this.toolStripComboBox_TxDataFormat_SelectionChangeCommitted);
        }

        

        private void oPENToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.PortName = cBoxCOMPORT.Text;
                serialPort1.BaudRate = Convert.ToInt32(cBoxBaudRate.Text);
                serialPort1.DataBits = Convert.ToInt32(cBoxDataBits.Text);
                serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cBoxStopBits.Text);
                serialPort1.Parity = (Parity)Enum.Parse(typeof(Parity), cBoxParityBits.Text);

                serialPort1.Open();
                progressBar1.Value = 100;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cLOSEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                progressBar1.Value = 0;
            }
        }


        private void btnSendData_Click(object sender, EventArgs e)
        {
            TxSendData();
        }

        private void toolStripComboBox2_DropDownClosed(object sender, EventArgs e)
        {
            //None
            //Both
            //New Line
            //Carriage Return
            if (toolStripComboBox_TxEndLine.Text == "None")
                sendWith = "None";
            else if (toolStripComboBox_TxEndLine.Text == @"Both (\r\n)")
                sendWith = @"Both (\r\n)";
            else if (toolStripComboBox_TxEndLine.Text == @"New Line (\n)")
                sendWith = @"New Line (\n)";
            else if (toolStripComboBox_TxEndLine.Text == @"Carriage Return (\r)")
                sendWith = @"Carriage Return (\r)";
        }

        private void chBoxDtrEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (chBoxDtrEnable.Checked)
            {
                serialPort1.DtrEnable = true;
                MessageBox.Show("DTR Enable", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
                serialPort1.DtrEnable = false;
        }

        private void chBoxRTSEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (chBoxRTSEnable.Checked)
            {
                serialPort1.RtsEnable = true;
                MessageBox.Show("RTS Enable", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
                serialPort1.RtsEnable = false;
        }

        private void tBoxDataOut_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                this.doSomething();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        
        private void doSomething()
        {
            TxSendData();
        }
        
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //dataIN = serialPort1.ReadExisting();

            List<int> dataBuffer = new List<int>();

            while (serialPort1.BytesToRead > 0)
            {
                try
                {
                    dataBuffer.Add(serialPort1.ReadByte());
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }
            dataINLength = dataBuffer.Count();
            dataInDec = new int[dataINLength];
            dataInDec = dataBuffer.ToArray();

            // 데이터 시리얼을 텍스트 박스로
            this.Invoke(new EventHandler(ShowData));
        }

        private void ShowData(object sender, EventArgs e)
        {
            // 입력 데이터 길이
            //int dataINLength = dataIN.Length;

            dataIN = RxDataFormat(dataInDec);
            lblDataInLength.Text = string.Format("{0:00}", dataINLength);
            if (toolStripComboBox_RxShowDataWith.Text == "Always Update") tBoxDataIN.Text = dataIN;  //dataIN에 저장된 데이터를 tBoxDataIN에 표시
            else if (toolStripComboBox_RxShowDataWith.Text == "Add to Old Data")
            {
                if (toolStripComboBox_RxDataPosition.Text == "TOP")
                    tBoxDataIN.Text = tBoxDataIN.Text.Insert(0, dataIN);  //dataIN에 저장된 데이터를 tBoxDataIN에 계속 추가하여 표시
                else if (toolStripComboBox_RxDataPosition.Text == "BOTTOM")
                    tBoxDataIN.Text += dataIN;
            }
            SaveDataToTxtFile();
            SaveDataToMySqlDatabase();
        }

        private void clearToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (tBoxDataIN.Text != "") 
                tBoxDataIN.Text = "";
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("열심히 공부하고 있습니다.\n무엇이든 해낼 겁니다.", "취준생", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            groupBox12.Width = panel1.Width - 212;
            groupBox12.Height = panel1.Height - 74;

            tBoxDataIN.Height = panel1.Height - 129;
        }

        private void toolStripComboBox_appendOrOverwriteText_DropDownClosed(object sender, EventArgs e)
        {
            if (toolStripComboBox_appendOrOverwriteText.Text == "Append Text")
                state_AppendText = true;
            else
                state_AppendText = false;
        }

        private void showDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 objForm2 = new Form2(this);
            objForm2.Show();
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Form3 objForm3 = new Form3(this);
            objForm3.Show();
            this.Hide();
        }

        private void tBoxDataIN_TextChanged(object sender, EventArgs e)
        {
            if (toolStripComboBox_RxDataPosition.Text == "BOTTOM")
            {
                tBoxDataIN.SelectionStart = tBoxDataIN.Text.Length;
                tBoxDataIN.ScrollToCaret();
            }
        }

        private void cBoxCOMPORT_DropDown(object sender, EventArgs e)
        {
            //현재 컴퓨터에 연결된 모든 시리얼 포트의 이름을 문자열 배열로 반환함(네임스페이스 System.IO.Ports필요)
            string[] ports = SerialPort.GetPortNames();   
            cBoxCOMPORT.Items.Clear();
            cBoxCOMPORT.Items.AddRange(ports);      //ports에 들어있는 모든 문자열을 콤보박스에 추가
        }

        private void tBoxDataOut_KeyPress(object sender, KeyPressEventArgs e)
        {
            char c = e.KeyChar;
            if (toolStripComboBox_TxDataFormat.Text == "Hex")
            {
                //Hex 형식에서, 텍스트 박스는 0-9 및 A-F 키만 허용
                //소문자는 대문자로 변환되므로, 두 가지 모두 텍스트 박스에 입력 가능
                char uppercase = char.ToUpper(c);

                //숫자 키가 눌리지 않았고, 백스페이스 키가 눌리지 않았으며, 삭제 키가 눌리지 않았고,
                //쉼표 키가 눌리지 않았으며, A-F 키가 눌리지 않은 경우
                if (!char.IsDigit(uppercase) && uppercase != 8 && uppercase != 46 && uppercase != ',' && !(uppercase >= 65 && uppercase <= 70))
                {
                    // KeyPress 이벤트를 취소
                    e.Handled = true;
                }
            }
            else if (toolStripComboBox_TxDataFormat.Text == "Decimal")
            {
                //Decimal 형식에서는, 텍스트 박스가 숫자 키만 허용. 즉, 0-9까지의 숫자만 입력가능
                //숫자 키가 눌리지 않았고, 백스페이스 키가 눌리지 않았으며, 삭제 키가 눌리지 않았고,
                //쉼표 키가 눌리지 않았을 때
                if (!char.IsDigit(c) && c != 8 && c != 46 && c != ',')
                {
                    // KeyPress 이벤트를 취소
                    e.Handled = true;
                }
            }
            else if (toolStripComboBox_TxDataFormat.Text == "Binary")
            {
                //Binary 형식에서는, 텍스트 박스가 숫자 0과1만 허용.
                //숫자 0키가 눌리지 않았고, 숫자1 키가 눌리지 않았으며, 삭제 키가 눌리지 않았고,
                //쉼표 키가 눌리지 않았을 때
                if (c != 49 && c != 48 && c != 8 && c != 46 && c != ',')
                {
                    // KeyPress 이벤트를 취소
                    e.Handled = true;
                }
            }
            else if (toolStripComboBox_TxDataFormat.Text == "Char") { }     //아무일도 없었다.
        }

        private void toolStripComboBox_TxDataFormat_SelectionChangeCommitted(object sender, EventArgs e)
        {
            //텍스트 박스에서 다른 TX 데이터 형식을 선택할 때마다, 텍스트 박스의 모든 내용을 삭제.
            tBoxDataOut.Clear();

            //매번 다른 TX 데이터 형식을 선택할 때마다 메시지를 표시
            string message = "If you are not using char data format, append the comma (,) after each byte data Otherwise, the byte data will ignore. \n" +
                "Example :\t255, -> One byte data \n" +
                "\t255,128,140, -> Two or more byte data \n" +
                "\t120,144,189, -> The 189 will ignore cause has no comma (,)";
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK);
        }
        #endregion

        
    }
}
