using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace XMLEditor.Controllers
{
    public class HomeController : Controller
    {
        string connectionString = "server=localhost;user id=root;database=xmleditor";

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Edit()
        {
            var list = new System.Collections.Generic.List<SelectListItem>
    {
        new SelectListItem{ Text="elise-config-externalSourceMapping.xml", Value = "elise-config-externalSourceMapping.xml" },
        new SelectListItem{ Text="elise-config-sharingMapping.xml", Value = "elise-config-sharingMapping.xml" },
        new SelectListItem{ Text="ELISE_COURRIERS_54.xml", Value = "ELISE_COURRIERS_54.xml"},
        new SelectListItem{ Text="xml", Value = "xml"},
    };
            ViewBag.dropdownListId = list;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult Submit(string file_name, string xml_data)
        {
            Session["file_name"] = file_name;
            System.Diagnostics.Debug.WriteLine("red " + file_name);

            //save changes in database
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                string query = "INSERT INTO HISTORIQUE (file_name, xml_data, modified_in) VALUES(@file_name,@xml_data,@modified_in)";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@file_name", file_name);
                cmd.Parameters.AddWithValue("@xml_data", xml_data);
                cmd.Parameters.AddWithValue("@modified_in", DateTime.Now);
                cmd.ExecuteNonQuery();
                con.Close();
            }
            //save changes in the origin file
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml_data);
            doc.Save("C:/Users/Rym/Documents/XMLEditor/XMLEditor/XMLEditor/fichier_xml" + file_name);
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
            DataTable historiqueTable = new DataTable();
            DataTable historiqueTable1 = new DataTable();
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT modified_in FROM HISTORIQUE where file_name=@file_name";
                MySqlDataAdapter adap = new MySqlDataAdapter(query, con);
                System.Diagnostics.Debug.WriteLine("red " + Session["file_name"]);
                adap.SelectCommand.Parameters.AddWithValue("@file_name", Session["file_name"]);
                adap.Fill(historiqueTable);
                var list = new System.Collections.Generic.List<SelectListItem>();
                for(int i=0;i<historiqueTable.Rows.Count;i++)
                {
                    System.Diagnostics.Debug.WriteLine("red " + historiqueTable.Rows[i][0].ToString());
                    list.Add(new SelectListItem { Text = historiqueTable.Rows[i][0].ToString(), Value = historiqueTable.Rows[i][0].ToString() });
                };
                ViewBag.versionDropdownList = list;

                string query1 = "SELECT xml_data FROM HISTORIQUE where file_name=@file_name ORDER BY num DESC LIMIT 1";
                MySqlDataAdapter adap1 = new MySqlDataAdapter(query1, con);
                adap1.SelectCommand.Parameters.AddWithValue("@file_name", Session["file_name"]);
                adap1.Fill(historiqueTable1);
                System.Diagnostics.Debug.WriteLine("blue " + historiqueTable1.Rows.Count);
                TempData["actual_xml"] = historiqueTable1.Rows[0][0];
                con.Close();
            }
            System.Diagnostics.Debug.WriteLine("blue1 " + TempData["actual_xml"]);

            return View(historiqueTable);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult Comparaison(string version_selected)
        {
            System.Diagnostics.Debug.WriteLine("green " + version_selected);
            DataTable historiqueTable = new DataTable();
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT xml_data FROM HISTORIQUE where file_name=@file_name and modified_in=@modified_in";
                MySqlDataAdapter adap = new MySqlDataAdapter(query, con);
                adap.SelectCommand.Parameters.AddWithValue("@file_name", Session["file_name"]);
                adap.SelectCommand.Parameters.AddWithValue("@modified_in", Convert.ToDateTime(version_selected));
                adap.Fill(historiqueTable);
                System.Diagnostics.Debug.WriteLine("rose " + historiqueTable.Rows.Count);
                TempData["xml_selected"] = historiqueTable.Rows[0][0];
                System.Diagnostics.Debug.WriteLine("rose " + historiqueTable.Rows[0][0]);
                con.Close();
            }

            return PartialView("Comparaison");
        }
    }
}