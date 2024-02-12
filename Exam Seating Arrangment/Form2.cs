using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Exam_Seating_Arrangment
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void InsertClassRoomsInDB(SqlConnection con)
        {
            // Insert data into Classroom table
            InsertClassroom(con, 501, 20);
            InsertClassroom(con, 502, 16);
            InsertClassroom(con, 505, 16);
            InsertClassroom(con, 701, 21);
            InsertClassroom(con, 702, 21);
            InsertClassroom(con, 703, 17);
            InsertClassroom(con, 705, 17);
        }

        private void InsertClassroom(SqlConnection con, int RoomNumber, int Capacity)
        {
            string query = "INSERT INTO Classroom (room_number, capacity) VALUES (@RoomNumber, @Capacity)";
            using (SqlCommand command = new SqlCommand(query, con))
            {
                command.Parameters.AddWithValue("@RoomNumber", RoomNumber);
                command.Parameters.AddWithValue("@Capacity", Capacity);
                command.ExecuteNonQuery();
            }
        }

        private void InsertStudentSInDB(SqlConnection con)
        {
            //dictionary to store student data
            Dictionary<long, string> BAFStudents = new Dictionary<long, string>();
            Dictionary<long, string> BAFHonsStudents = new Dictionary<long, string>();
            Dictionary<long, string> BBAFinanceStudents = new Dictionary<long, string>();
            Dictionary<long, string> BBAHRStudents = new Dictionary<long, string>();
            Dictionary<long, string> BBAMarketingStudents = new Dictionary<long, string>();

            //fill dictionaries with data
            for (long rollnumber = 31011122001; rollnumber <= 31011122256; rollnumber++)
            {
                if (rollnumber >= 31011122001 && rollnumber <= 31011122072)
                    BAFStudents.Add(rollnumber, "baf");
                else if (rollnumber >= 31011122073 && rollnumber <= 31011122104)
                    BAFHonsStudents.Add(rollnumber, "baf hons");
                else if (rollnumber >= 31011122105 && rollnumber <= 31011122172)
                    BBAHRStudents.Add(rollnumber, "bba finance");
                else if (rollnumber >= 31011122173 && rollnumber <= 31011122188)
                    BBAFinanceStudents.Add(rollnumber, "bba hr");
                else
                    BBAMarketingStudents.Add(rollnumber, "bba marketing");
            }

            // Insert data into Students table
            InsertStudents(con, BAFStudents);
            InsertStudents(con, BAFHonsStudents);
            InsertStudents(con, BBAFinanceStudents);
            InsertStudents(con, BBAHRStudents);
            InsertStudents(con, BBAMarketingStudents);
        }

        private void InsertStudents(SqlConnection con, Dictionary<long, string> students)
        {
            // Method to insert student data into Students table
            foreach (var student in students)
            {
                string query = "INSERT INTO Student (RollNumber, Course) VALUES (@RollNumber, @Course)";
                using (SqlCommand command = new SqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@RollNumber", student.Key);
                    command.Parameters.AddWithValue("@Course", student.Value);
                    command.ExecuteNonQuery();
                }
            }
        }
        private Dictionary<string, int> FetchClassroomDataFromDatabase(string connectionString)
        {
            // Connect to your SQL database and fetch data
            // This is a simplified example assuming you have a table named Classrooms
            // with columns ClassroomNumber and LoopCondition
            Dictionary<string, int> classroomData = new Dictionary<string, int>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string RoomFilter = "501";//textBox2.Text;
                string query = "SELECT RoomNumber, Capacity FROM Classroom";
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                command.Parameters.AddWithValue("@RoomNumber", RoomFilter);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string RoomNumber = "Room No. :" + reader["RoomNumber"].ToString();
                    int loopCondition = Convert.ToInt32(reader["Capacity"]);
                    classroomData[RoomNumber] = loopCondition;
                }
                reader.Close();
            }

            return classroomData;
        }

    }
}
