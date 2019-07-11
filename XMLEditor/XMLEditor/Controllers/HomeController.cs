using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace XMLEditor.Controllers
{
    public class HomeController : Controller
    {
        string ConnectionString = "server=localhost;user id=root;database=xmleditor";

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
            System.Diagnostics.Debug.WriteLine("red " + file_name);
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
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
            //check for reportName parameter value now
            //to do : Return something
            return RedirectToAction("Affiche", "Home"); ;
        }

        public ActionResult Affiche()
        {
            DataTable historiqueTable = new DataTable();
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                con.Open();
                MySqlDataAdapter adap = new MySqlDataAdapter("SELECT * FROM HISTORIQUE", con);
                adap.Fill(historiqueTable);
                con.Close();
            }
            return View(historiqueTable);
        }
    }
}