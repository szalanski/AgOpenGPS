// Updated version of FormFieldISOXML with logic delegated to IsoXmlFieldBuilder
using AgLibrary.Logging;
using AgOpenGPS.Controls;
using AgOpenGPS.Core;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Translations;
using AgOpenGPS.Forms;
using AgOpenGPS.Helpers;
using AgOpenGPS.Protocols.ISOBUS;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace AgOpenGPS
{
    public partial class FormFieldISOXML : Form
    {
        private readonly FormGPS mf;
        private XmlDocument iso;
        private string xmlFilename;
        private XmlNodeList pfd;
        private int idxFieldSelected;

        public FormFieldISOXML(Form _callingForm)
        {
            mf = _callingForm as FormGPS;
            InitializeComponent();
        }

        private void FormFieldISOXML_Load(object sender, EventArgs e)
        {
            tboxFieldName.Text = "";
            btnBuildFields.Enabled = false;
            labelFieldname.Text = gStr.gsEditFieldName;
            this.Text = gStr.gsCreateNewFromIsoXML;
            labelField.Text = gStr.gsBasedOnField;
            tree.Nodes?.Clear();

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "XML files (*.XML)|*.XML",
                InitialDirectory = RegistrySettings.fieldsDirectory
            };

            if (ofd.ShowDialog() != DialogResult.Cancel)
            {
                xmlFilename = ofd.FileName;
                iso = new XmlDocument { PreserveWhitespace = false };
                iso.Load(xmlFilename);

                pfd = iso.GetElementsByTagName("PFD");
                int index = 0;

                try
                {
                    foreach (XmlNode nodePFD in pfd)
                    {
                        double area;
                        double.TryParse(nodePFD.Attributes["D"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out area);
                        area *= 0.0001;

                        tree.Nodes.Add(nodePFD.Attributes["C"].Value + " Area: " + area + " Ha  " + nodePFD.Attributes["A"].Value);
                        tree.Nodes[tree.Nodes.Count - 1].Tag = index++;

                        XmlNodeList fieldParts = nodePFD.ChildNodes;
                        TreeNode fieldNode = tree.Nodes[tree.Nodes.Count - 1]; // laatste veld-node

                        // GGP-based guidance
                        foreach (XmlNode nodePart in fieldParts)
                        {
                            if (nodePart.Name == "GGP")
                            {
                                XmlNode gpn = nodePart.SelectSingleNode("GPN");
                                if (gpn != null && gpn.Attributes["B"] != null && gpn.Attributes["C"] != null)
                                {
                                    string name = gpn.Attributes["B"].Value;
                                    string type = gpn.Attributes["C"].Value;

                                    if (type == "1") // AB-line
                                    {
                                        TreeNode abNode = new TreeNode("AB-" + name);
                                        abNode.ForeColor = System.Drawing.Color.Green;
                                        fieldNode.Nodes.Add(abNode);
                                    }
                                    else if (type == "3") // Curve
                                    {
                                        TreeNode curveNode = new TreeNode("Curve-" + name);
                                        curveNode.ForeColor = System.Drawing.Color.Orange;
                                        fieldNode.Nodes.Add(curveNode);
                                    }
                                }
                            }
                        }

                        // LSG-only guidance (v3 stijl)
                        foreach (XmlNode nodePart in fieldParts)
                        {
                            if (nodePart.Name == "LSG" && nodePart.Attributes["A"] != null && nodePart.Attributes["A"].Value == "5")
                            {
                                string name = nodePart.Attributes["B"] != null ? nodePart.Attributes["B"].Value : "Unnamed";
                                XmlNodeList pnts = nodePart.SelectNodes("PNT");
                                int pointCount = pnts != null ? pnts.Count : 0;

                                if (pointCount == 2)
                                {
                                    TreeNode abNode = new TreeNode("AB-" + name);
                                    abNode.ForeColor = System.Drawing.Color.Green;
                                    fieldNode.Nodes.Add(abNode);
                                }
                                else if (pointCount > 2)
                                {
                                    TreeNode curveNode = new TreeNode("Curve-" + name);
                                    curveNode.ForeColor = System.Drawing.Color.Orange;
                                    fieldNode.Nodes.Add(curveNode);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.EventWriter("Creating new iso field " + ex.ToString());
                    FormDialog.Show(gStr.gsError, ex.ToString(), MessageBoxButtons.OK);
                    return;
                }

                if (tree.Nodes.Count == 0)
                {
                    btnBuildFields.Enabled = false;
                }

                tree.Sort();
            }
            else
            {
                Close();
            }

            if (!ScreenHelper.IsOnScreen(Bounds))
            {
                Top = 0;
                Left = 0;
            }
        }


        private void tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (tree.SelectedNode?.Parent == null)
            {
                idxFieldSelected = (int)tree.SelectedNode.Tag;
            }
            else
            {
                idxFieldSelected = (int)tree.SelectedNode.Parent.Tag;
            }

            if (idxFieldSelected >= 0)
            {
                labelField.Text = idxFieldSelected + " " + pfd[idxFieldSelected].Attributes["C"].Value;
                tboxFieldName.Text = pfd[idxFieldSelected].Attributes["C"].Value;
                btnBuildFields.Enabled = true;
                btnAddDate.Enabled = true;
                btnAddTime.Enabled = true;
                tboxFieldName.Enabled = true;
            }
            else
            {
                btnBuildFields.Enabled = false;
                btnAddDate.Enabled = false;
                btnAddTime.Enabled = false;
                tboxFieldName.Enabled = false;
            }
        }

        private async void btnBuildFields_Click(object sender, EventArgs e)
        {
            mf.currentFieldDirectory = tboxFieldName.Text.Trim();
            string directoryPath = Path.Combine(RegistrySettings.fieldsDirectory, mf.currentFieldDirectory);

            if (Directory.Exists(directoryPath))
            {
                FormDialog.Show(gStr.gsDirectoryExists, gStr.gsChooseADifferentName, MessageBoxButtons.OK);
                mf.currentFieldDirectory = "";
                return;
            }

            var fieldParts = pfd[idxFieldSelected].ChildNodes;
            var builder = new IsoXmlFieldBuilder(fieldParts, mf.currentFieldDirectory, mf.AppModel, mf);

            if (!builder.TryExtractOrigin(out var origin))
            {
                mf.YesMessageBox("Can't calculate center of field. Missing Outer Boundary or AB line.");
                return;
            }

            mf.JobNew();
            mf.pn.DefineLocalPlane(origin, true);

            if (!mf.isJobStarted)
            {
                mf.TimedMessageBox(3000, gStr.gsFieldNotOpen, gStr.gsCreateNewField);
                return;
            }

            builder.TryBuildBoundaries();
            builder.TryBuildHeadland();
            builder.TryBuildGuidanceLines();
            builder.SaveFieldFiles(directoryPath);
            builder.FinalizeField();

            if (mf.bnd.bndList.Count > 0) mf.btnABDraw.Visible = true;
            mf.FieldMenuButtonEnableDisable(mf.bnd.bndList.Count > 0 && mf.bnd.bndList[0].hdLine.Count > 0);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void tboxFieldName_TextChanged(object sender, EventArgs e)
        {
            TextBox textboxSender = (TextBox)sender;
            int cursorPosition = textboxSender.SelectionStart;
            textboxSender.Text = Regex.Replace(textboxSender.Text, glm.fileRegex, "");
            textboxSender.SelectionStart = cursorPosition;
        }

        private void tboxFieldName_Click(object sender, EventArgs e)
        {
            if (mf.isKeyboardOn)
            {
                ((TextBox)sender).ShowKeyboard(this);
                btnSerialCancel.Focus();
            }
        }

        private void btnSerialCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnAddDate_Click(object sender, EventArgs e)
        {
            tboxFieldName.Text += " " + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private void btnAddTime_Click(object sender, EventArgs e)
        {
            tboxFieldName.Text += " " + DateTime.Now.ToString("HH-mm", CultureInfo.InvariantCulture);
        }
    }
}
