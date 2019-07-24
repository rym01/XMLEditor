using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Microsoft.XmlDiffPatch;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;

namespace XMLEditor.Controllers
{
    public class HomeController : Controller
    {
        readonly string connectionString = "server=localhost;user id=root;database=xmleditor";
        public static string diffile;
        XmlDiffOptions diffOptions = new XmlDiffOptions();
        XmlDiff diff = new XmlDiff();

        public ActionResult Index(string logout)
        {
            if (logout != null)
            {
                if (logout.Equals("true"))
                {
                    Session["user_name"] = null;
                }
            }
            var list = new System.Collections.Generic.List<SelectListItem>
    {
        new SelectListItem{ Text="elise-config-externalSourceMapping.xml", Value = "elise-config-externalSourceMapping.xml" },
        new SelectListItem{ Text="elise-config-sharingMapping.xml", Value = "elise-config-sharingMapping.xml" },
        new SelectListItem{ Text="ELISE_COURRIERS_54.xml", Value = "ELISE_COURRIERS_54.xml"}
    };
            ViewBag.dropdownListId = list;

            return View();
        }

        public ActionResult EditOrCompare(string file_name)
        {
            Session["file_name"] = file_name;
            return PartialView("SignIn");
        }

        public ActionResult Edit()
        {
            if (Session["file_name"] != null)
            {
                return View();
            }
            return RedirectToAction("Index", "Home"); ;
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult SaveUser(string user_name)
        {
            Session["user_name"] = user_name;
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult Submit(string xml_data)
        {

            //save changes in database
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                string query = "INSERT INTO HISTORIQUE (file_name, xml_data, modified_in,modified_by) VALUES(@file_name,@xml_data,@modified_in,@modified_by)";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@file_name", Session["file_name"]);
                cmd.Parameters.AddWithValue("@xml_data", xml_data);
                cmd.Parameters.AddWithValue("@modified_in", DateTime.Now);
                cmd.Parameters.AddWithValue("@modified_by", Session["user_name"]);
                cmd.ExecuteNonQuery();
                con.Close();
            }
            //add to the log file
            SaveLog();
            //add diffile in ChangeLog
            diffile = "C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/ChangeLog/" + Session["file_name"] + "_" +  DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_" + Session["user_name"]+".xml";
          
            string file1 = "C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/fichier_xml/" + Session["file_name"];
            string file2 = "C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/fichier2.xml";
             XmlDocument doc2 = new XmlDocument();
             doc2.LoadXml(xml_data);
             doc2.Save(file2);
             bool isEqual = DoCompare(file1, file2);
             /*if (!isEqual)
             {
                 StringWriter sw = new StringWriter();

                 XmlTextWriter writer = new XmlTextWriter(sw);

                 writer.Formatting = Formatting.Indented;

                 XmlDocument originalDoc = new XmlDocument();

                 originalDoc.Load(file1);

                 XmlPatch patch = new XmlPatch();

                 XmlTextReader reader = new XmlTextReader(new StringReader(diffile));

                 //Perform patch operation

                 patch.Patch(originalDoc, reader);

                 originalDoc.Save(writer);

                 reader.Close();

             }*/
             //save changes in origin file
            string ch = " xml:space='preserve'";
            while (xml_data.Contains(ch))
            {
                xml_data = xml_data.Remove(xml_data.IndexOf(ch), ch.Length);
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml_data);
            doc.Save(file1);

            return RedirectToAction("AfficheComparaison", "Home");
        }

        public ActionResult Affiche()
        {
            DataTable historiqueTable = new DataTable();
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                MySqlDataAdapter adap = new MySqlDataAdapter("SELECT * FROM HISTORIQUE", con);
                adap.Fill(historiqueTable);
                con.Close();
            }
            return View(historiqueTable);
        }

        public ActionResult AfficheComparaison()
        {
            if (Session["file_name"] != null)
            {
                DataTable historiqueTable = new DataTable();
                DataTable historiqueTable1 = new DataTable();
                using (MySqlConnection con = new MySqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT modified_in FROM HISTORIQUE where file_name=@file_name";
                    MySqlDataAdapter adap = new MySqlDataAdapter(query, con);
                    adap.SelectCommand.Parameters.AddWithValue("@file_name", Session["file_name"]);
                    adap.Fill(historiqueTable);
                    var list = new System.Collections.Generic.List<SelectListItem>();
                    for (int i = 0; i < historiqueTable.Rows.Count - 1; i++)
                    {
                        list.Add(new SelectListItem { Text = historiqueTable.Rows[i][0].ToString(), Value = historiqueTable.Rows[i][0].ToString() });
                    };
                    ViewBag.versionDropdownList = list;

                    string query1 = "SELECT xml_data FROM HISTORIQUE where file_name=@file_name ORDER BY num DESC LIMIT 1";
                    MySqlDataAdapter adap1 = new MySqlDataAdapter(query1, con);
                    adap1.SelectCommand.Parameters.AddWithValue("@file_name", Session["file_name"]);
                    adap1.Fill(historiqueTable1);
                    Session["actual_xml"] = historiqueTable1.Rows[0][0];
                    con.Close();
                }

                return View(historiqueTable);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult Comparaison(string version_selected)
        {
            DataTable historiqueTable = new DataTable();
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT xml_data FROM HISTORIQUE where file_name=@file_name and modified_in=@modified_in";
                MySqlDataAdapter adap = new MySqlDataAdapter(query, con);
                adap.SelectCommand.Parameters.AddWithValue("@file_name", Session["file_name"]);
                adap.SelectCommand.Parameters.AddWithValue("@modified_in", Convert.ToDateTime(version_selected));
                adap.Fill(historiqueTable);
                Session["xml_selected"] = historiqueTable.Rows[0][0];
                con.Close();
            }

            return PartialView("Comparaison");
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult Compare()
        {
            string file2 = "C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/fichier2.xml";
            string ch = " xml:space='preserve'";
            string xml_data = (string)Session["xml_selected"];
            while (xml_data.Contains(ch))
            {
                xml_data = xml_data.Remove(xml_data.IndexOf(ch), ch.Length);
            }
            XmlDocument doc2 = new XmlDocument();
            doc2.LoadXml((string)Session["xml_selected"]);
            doc2.Save(file2);
            string file1 = "C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/fichier_xml/" + Session["file_name"];
            bool isEqual = DoCompare(file2, file1);
            if (isEqual)
            {
                System.Diagnostics.Debug.WriteLine("Files Identical for the given options");

                return PartialView("Equal");
            }

            //Files were not equal, so construct XmlDiffView.
            XmlDiffView dv = new XmlDiffView();

            //Load the original file again and the diff file.
            XmlTextReader orig = new XmlTextReader(file2);
            XmlTextReader diffGram = new XmlTextReader(diffile);
            dv.Load(orig, diffGram);

            //Wrap the HTML file with necessary html and 
            //body tags and prepare it before passing it to the GetHtml method.
            string tempFile = "C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/Views/Home/Compare.cshtml";
            StreamWriter sw1 = new StreamWriter(tempFile);

            sw1.Write("<style>td,th{ border:none;} .cadre {background-color: #1D1B1B;border-radius: 25px; padding:25px;} .my-custom-scrollbar {position: relative;height: 350px; overflow: auto;} .table-wrapper-scroll-y {display: block;} .table{ background:white; width:100%}</style><div class=\"cadre\"><div class=\"table-wrapper-scroll-y my-custom-scrollbar\"><table class=\"table\" > ");
            //Write Legend.
            sw1.Write("<tr><td colspan='2' align='center'><b>Legend:</b> <font style='background-color: yellow'" +
                " color='black'>added</font>&nbsp;&nbsp;<font style='background-color: red'" +
                " color='black'>removed</font>&nbsp;&nbsp;<font style='background-color: " +
                "lightgreen' color='black'>changed</font>&nbsp;&nbsp;" +
                "<font style='background-color: red' color='blue'>moved from</font>" +
                "&nbsp;&nbsp;<font style='background-color: yellow' color='blue'>moved to" +
                "</font>&nbsp;&nbsp;<font style='background-color: white' color='#AAAAAA'>" +
                "ignored</font></td></tr>");

            sw1.Write("<tr><td><b>Choosen Version");
            sw1.Write("</b></td><td><b>Current Version");
            sw1.Write("</b></td></tr>");
            //This gets the differences but just has the 
            //rows and columns of an HTML table
            dv.GetHtml(sw1);
            //Finish wrapping up the generated HTML and complete the file.
            sw1.Write("</table></div></div>");
            //HouseKeeping...close everything we dont want to lock.
            sw1.Close();
            dv = null;
            orig.Close();
            diffGram.Close();
            //System.IO.File.Delete(diffile);

            return PartialView("Compare");
        }

        private void SetDiffOptions()
        {
            diffOptions = XmlDiffOptions.None;
            diffOptions = diffOptions | XmlDiffOptions.IgnoreChildOrder;
            diffOptions = diffOptions | XmlDiffOptions.IgnoreComments;
            diffOptions = diffOptions | XmlDiffOptions.IgnoreWhitespace;
            diff.Options = diffOptions;
        }

        bool DoCompare(string file1, string file2)
        {
            System.Diagnostics.Debug.WriteLine("red" + diffile.ToString());
            XmlTextWriter tw = new XmlTextWriter(new StreamWriter(diffile));
            tw.Formatting = Formatting.Indented;
            SetDiffOptions();
            bool isEqual = false;

            try
            {
                isEqual = diff.Compare(file1, file2, false, tw);
            }
            catch (XmlException xe)
            {
                System.Diagnostics.Debug.WriteLine("An exception occured while comparing\n" + xe.StackTrace);
            }
            finally
            {
                tw.Close();
            }
            return isEqual;
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult RetrieveChoosenVersion()
        {
            string file1 = "C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/fichier_xml/" + Session["file_name"];
            XmlDocument doc2 = new XmlDocument();
            doc2.LoadXml((string)Session["xml_selected"]);
            doc2.Save(file1);
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                string query = "INSERT INTO HISTORIQUE (file_name, xml_data, modified_in,modified_by) VALUES(@file_name,@xml_data,@modified_in,@modified_by)";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@file_name", (string)Session["file_name"]);
                cmd.Parameters.AddWithValue("@xml_data", (string)Session["xml_selected"]);
                cmd.Parameters.AddWithValue("@modified_in", DateTime.Now);
                cmd.Parameters.AddWithValue("@modified_by", Session["user_name"]);
                cmd.ExecuteNonQuery();
                con.Close();
            }
            SaveLog();
            return RedirectToAction("Index","Home");
        }

        public void SaveLog()
        {
            string log = "C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/Log.txt";
            using (StreamWriter file =
            new StreamWriter(log, true))
            {
                file.WriteLine(DateTime.Now+" | Modification du fichier xml "+ Session["file_name"] + " | par " +Session["user_name"]+" voir le dossier Changelog à cette date pour avoir un aperçu du changement.");
            }
        }
    }
}