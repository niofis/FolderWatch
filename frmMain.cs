using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using CustomUIControls;
using System.Diagnostics;
using System.Threading;
using System.Net.Mail;
using System.Net;

namespace FolderWatch
{
    public partial class frmMain : Form
    {
        private Dictionary<string, FileSystemWatcher> watchers;
        private int position = 0;

        public frmMain()
        {
            InitializeComponent();
            watchers = new Dictionary<string, FileSystemWatcher>();
            //Cargar Configuracion
            CargarConfig();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //Guardar Configuracion
            GuardarConfig();
            base.OnClosing(e);
        }

        private void CargarConfig()
        {
            if (!File.Exists("config.cfg"))
                return;
            StreamReader str = new StreamReader("config.cfg");
            string linea = str.ReadLine();
            Regex config=new Regex("\\<(?<nombre>.+?)\\>\\((?<valor>.+?)\\)");
            Match m;
            while (linea != "" && linea!=null)
            {
                if (config.IsMatch(linea))
                {
                    m = config.Match(linea);
                    if (m.Groups["nombre"].Value.ToLower() == "ee")
                        chkEmail.Checked = Boolean.Parse(m.Groups["valor"].Value);
                    if (m.Groups["nombre"].Value.ToLower() == "ma")
                        chkAlerta.Checked = Boolean.Parse(m.Groups["valor"].Value);
                    if (m.Groups["nombre"].Value.ToLower() == "rs")
                        chkSonido.Checked = Boolean.Parse(m.Groups["valor"].Value);
                    if (m.Groups["nombre"].Value.ToLower() == "cr")
                        chkCrear.Checked = Boolean.Parse(m.Groups["valor"].Value);
                    if (m.Groups["nombre"].Value.ToLower() == "el")
                        chkEliminar.Checked = Boolean.Parse(m.Groups["valor"].Value);
                    if (m.Groups["nombre"].Value.ToLower() == "mo")
                        chkModificar.Checked = Boolean.Parse(m.Groups["valor"].Value);
                    if (m.Groups["nombre"].Value.ToLower() == "emails")
                        txtEmail.Text = m.Groups["valor"].Value;
                    if (m.Groups["nombre"].Value.ToLower() == "servidor")
                        txtServidor.Text = m.Groups["valor"].Value;
                    if (m.Groups["nombre"].Value.ToLower() == "ext")
                        txtExtension.Text = m.Groups["valor"].Value;
                    if (m.Groups["nombre"].Value.ToLower() == "folder")
                        AgregaWatcher(m.Groups["valor"].Value);
                }

                linea = str.ReadLine();

            } 
            str.Close();
        }

        private void GuardarConfig()
        {
            StreamWriter str = new StreamWriter("config.cfg",false);
            string formato="<{0}>({1})";
            str.WriteLine(String.Format(formato, "ee", chkEmail.Checked.ToString()));
            str.WriteLine(String.Format(formato, "ma", chkAlerta.Checked.ToString()));
            str.WriteLine(String.Format(formato, "rs", chkSonido.Checked.ToString()));
            str.WriteLine(String.Format(formato, "cr", chkCrear.Checked.ToString()));
            str.WriteLine(String.Format(formato, "el", chkEliminar.Checked.ToString()));
            str.WriteLine(String.Format(formato, "mo", chkModificar.Checked.ToString()));
            str.WriteLine(String.Format(formato, "emails", txtEmail.Text));
            str.WriteLine(String.Format(formato, "servidor", txtServidor.Text));
            str.WriteLine(String.Format(formato, "ext", txtExtension.Text));

            foreach (Object obj in lstDirectorios.Items)
            {
                str.WriteLine(String.Format(formato, "folder",obj.ToString()));
            }

            str.Close();
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            if (dlgFolder.ShowDialog() == DialogResult.OK)
            {
                AgregaWatcher(dlgFolder.SelectedPath);
            }
        }

        private void AgregaWatcher(string folder)
        {
            if (!watchers.ContainsKey(folder))
            {
                lstDirectorios.Items.Add(folder);
                FileSystemWatcher fsw = new FileSystemWatcher(folder);
                fsw.Created += new FileSystemEventHandler(FileSystemWatcher_Event);
                fsw.Changed += new FileSystemEventHandler(FileSystemWatcher_Event);
                fsw.Renamed += new RenamedEventHandler(FileSystemWatcher_Event);
                fsw.Deleted += new FileSystemEventHandler(FileSystemWatcher_Event);
                fsw.EnableRaisingEvents = true;
                fsw.SynchronizingObject = this;
            }
        }



        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (lstDirectorios.SelectedItem != null)
            {
                string file=lstDirectorios.SelectedItem.ToString();
                FileSystemWatcher fsw = watchers[file];
                fsw.Dispose();
                watchers.Remove(file);
                lstDirectorios.Items.Remove(lstDirectorios.SelectedItem);

            }
        }

        private void FileSystemWatcher_Event(object sender, FileSystemEventArgs e)
        {
            
            string[] exts = txtExtension.Text.Replace("*","").Split(';');
            bool alert = false;
            foreach (string s in exts)
            {
                if (e.Name.Contains(s))
                {
                    alert = true;
                    break;
                }
            }
            if (!alert)
                return;
            if (chkCrear.Checked && e.ChangeType == WatcherChangeTypes.Created)
            {
                Aviso("Creado", e.Name, e.FullPath);
            }
            if (chkEliminar.Checked && e.ChangeType == WatcherChangeTypes.Deleted)
            {
                Aviso("Eliminado", e.Name, e.FullPath);
            }
            if (chkModificar.Checked && (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Renamed))
            {
                Aviso("Modificado", e.Name, e.FullPath);
            }
        }

        private void Aviso(string descripcion, string archivo, string direccion)
        {
            
            if (chkEmail.Checked)
            {
                if (txtServidor.Text != "" && txtEmail.Text!="")
                {
                    SmtpClient client = new SmtpClient(txtServidor.Text);
                    client.Credentials = new NetworkCredential("HMOMARTINREA\\ecarrillo", "Troya2016");

                    MailMessage mail = new MailMessage("FolderWatchAlert@FWA.x", txtEmail.Text);

                    mail.Subject = "Archivo " + descripcion + ": " + archivo ;

                    mail.Body = direccion;
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.IsBodyHtml = false;
                    try
                    {
                        client.Send(mail);
                    }
                    catch (Exception ex)
                    {
                        string es = ex.ToString();
                    }
                }
                
            }
            if (chkAlerta.Checked)
            {
                TaskbarNotifier tn = new TaskbarNotifier();
                tn.SetBackgroundBitmap("skin.bmp",
                        Color.FromArgb(255, 0, 255));
                tn.SetCloseBitmap("close.bmp",
                        Color.FromArgb(255, 0, 255), new Point(127, 8));
                tn.TitleRectangle = new Rectangle(40, 9, 70, 25);
                tn.ContentRectangle = new Rectangle(8, 20, 133, 88);
                tn.ReShowOnMouseOver = true;
                tn.StackPosition = position++;
                tn.ContentClick += new EventHandler(Notifier_ContentClick);
                tn.VisibleChanged += new EventHandler(Notifier_VisibleChanged);
                tn.Tag = direccion;
                tn.Visible = true;
                tn.Show(descripcion , archivo
                    , 250, 1500000000, 250);
                Thread.Sleep(1000);
            }
            if (chkSonido.Checked)
            {
                WSounds ws = new WSounds(); 
                ws.Play("beep.wav", ws.SND_FILENAME | ws.SND_ASYNC);
            }
        }

        void Notifier_VisibleChanged(object sender, EventArgs e)
        {
            TaskbarNotifier tn = sender as TaskbarNotifier;
            if (tn.Visible == false)
            {
                if (position == tn.StackPosition + 1)
                    position = 0;
            }
        }

        void Notifier_ContentClick(object sender, EventArgs e)
        {
            TaskbarNotifier tn = sender as TaskbarNotifier;
            string archivo = (string)tn.Tag;

            ProcessStartInfo process = new ProcessStartInfo();
            process.FileName = "explorer.exe";
            process.Arguments =  "/select," + archivo;
            process.RedirectStandardInput = false;
            process.RedirectStandardOutput = false;
            process.RedirectStandardError = false;
            process.CreateNoWindow = false;
            process.UseShellExecute = false;
            Process ex = Process.Start(process);
            tn.Close();
        }

        private void txtExtension_TextChanged(object sender, EventArgs e)
        {
        }

        private void txtExtension_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void btnAplicar_Click(object sender, EventArgs e)
        {

        }

   }
}
