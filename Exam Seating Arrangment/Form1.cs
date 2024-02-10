using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.VisualBasic.FileIO;


namespace Exam_Seating_Arrangment
{
    public partial class Form1 : Form
    {
        // Connection string for SQL Server
        string connectionString = "Data Source=TARUNJOSHI\\SQLEXPRESS;Initial Catalog=ExamCell;Integrated Security=True";

        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    //InsertClassRoomsInDB(con);
                    //InsertStudentSInDB(con);

                    //Fetch classroom data from the database
                    Dictionary<string, int> classroomData = FetchClassroomDataFromDatabase(connectionString);
                    // Create the classrooms dictionary
                    Dictionary<string, List<List<(long, string)>>> classrooms = new Dictionary<string, List<List<(long, string)>>>();
                    // Populate the classrooms dictionary based on the fetched data
                    foreach (var kvp in classroomData)
                    {
                        string classroomNumber = kvp.Key;
                        int loopCondition = kvp.Value;
                        classrooms[classroomNumber] = new List<List<(long, string)>>();
                        for (int i = 0; i < loopCondition; i++)
                        {
                            classrooms[classroomNumber].Add(new List<(long, string)>());
                        }
                    }
                    printLength();
                    ReadStudents(con, classrooms);
                }
                MessageBox.Show("Data successfully inserted into database.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void printLength()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string courseFilter = textBox1.Text;
                string query = "SELECT COUNT(*) AS CourseCount FROM Student";
                using (SqlCommand command = new SqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@Course", courseFilter);

                    // Execute the query and retrieve the result
                    int courseCount = (int)command.ExecuteScalar();

                    // Now you can use courseCount as needed
                    label1.Text = $"Number of students in course {courseFilter}: {courseCount}";
                }
            }
        }


        private void ReadStudents(SqlConnection con, Dictionary<string, List<List<(long, string)>>> classrooms)
        {
            using (FileStream fs = new FileStream("C://Users//Pulin//Desktop//Project", FileMode.Create))
            {
                Document document = new Document();
                PdfWriter.GetInstance(document, fs);
                document.Open();
                string courseFilter = textBox1.Text;
                string query = "SELECT RollNumber, Course FROM Student";
                using (SqlCommand command = new SqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@Course", courseFilter);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        //Console.WriteLine("Roll Number\tCourse");
                        //Console.WriteLine("------------------------");
                        while (reader.Read())
                        {
                            long rollNumber = reader.GetInt64(0);
                            string course = reader.GetString(1);
                            bool assigned = false;
                            foreach (var classroom in classrooms)
                            {
                                foreach (var bench in classroom.Value)
                                {
                                    if (bench.Count < 2)
                                    {
                                        //Console.WriteLine(CompareWithBenchsFirstStudent(course, bench));
                                        var studentTuple = (rollNumber, course);
                                        //Console.WriteLine(bench + " " + studentTuple);

                                        bench.Add(studentTuple);
                                        assigned = true;
                                        break;
                                    }
                                }
                                if (assigned)
                                    break;
                            }
                            if (!assigned)
                            {
                                document.Add(new Paragraph($"Unable to assign student {rollNumber} of {course}"));
                                Console.WriteLine($"{rollNumber}\t\t{course}");
                            }

                        }
                    }

                    foreach (var classroom in classrooms)
                    {
                        document.Add(new Paragraph("Room No: " + classroom.Key));
                        //Console.WriteLine(classroom.Value.Count * 2); // Print size of classroom so mul by 2

                        // Calculate & print starting and last roll number for each unique course
                        Dictionary<string, long> startingRollNumbers = new Dictionary<string, long>();
                        Dictionary<string, long> lastRollNumbers = new Dictionary<string, long>();
                        foreach (var bench in classroom.Value)
                        {
                            foreach (var student in bench)
                            {
                                string subject = student.Item2;
                                long rollNumber = student.Item1;

                                if (!startingRollNumbers.ContainsKey(subject))
                                {
                                    startingRollNumbers.Add(subject, rollNumber);
                                }
                                else
                                {
                                    startingRollNumbers[subject] = Math.Min(startingRollNumbers[subject], rollNumber);
                                }

                                // Update last roll number for each subject
                                if (!lastRollNumbers.ContainsKey(subject))
                                {
                                    lastRollNumbers.Add(subject, rollNumber);
                                }
                                else
                                {
                                    lastRollNumbers[subject] = Math.Max(lastRollNumbers[subject], rollNumber);
                                }
                            }
                        }
                        foreach (var entry in startingRollNumbers)
                        {
                            document.Add(new Paragraph($"Subject: {entry.Key}, Starting Roll Number: {entry.Value}, Last Roll Number: {lastRollNumbers[entry.Key]}"));
                        }

                        // Print students assigned to each bench
                        foreach (var bench in classroom.Value)
                        {
                            document.Add(new Paragraph("Bench:"));
                            foreach (var student in bench)
                            {
                                document.Add(new Paragraph($"  {student.Item1}: {student.Item2}"));
                            }
                        }
                        document.Add(new Paragraph());
                    }

                    document.Close();
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
                string RoomFilter = textBox2.Text;
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
            string query = "INSERT INTO Classroom (RoomNumber, Capacity) VALUES (@RoomNumber, @Capacity)";
            using (SqlCommand command = new SqlCommand(query, con))
            {
                command.Parameters.AddWithValue("@RoomNumber", RoomNumber);
                command.Parameters.AddWithValue("@Capacity", Capacity);
                command.ExecuteNonQuery();
            }
        }
        private bool CompareWithBenchsFirstStudent(object studentValue, List<(long, string)> bench)
        {
            if (bench.Count != 0)
            {
                if ((string)studentValue == bench.Last().Item2)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        private void CSVToDBDataInsertion()
        {
            // Replace these paths with the actual paths to your CSV files
            string studentCsvFilePath = @"C:\Users\Pulin\Downloads\eg - Sheet1.csv";
            string classroomCsvFilePath = @"C:\Users\Pulin\Downloads\eg - Sheet3.csv";
            string programmeCsvFilePath = @"C:\Users\Pulin\Downloads\eg - Sheet2.csv";
            string courseCsvFilePath = @"C:\Users\Pulin\Downloads\eg - Sheet4.csv";
            string programmeCoursesCsvFilePath = @"C:\Users\Pulin\Downloads\eg - Sheet5.csv";

            // Insert data from CSV files into respective tables
            InsertStudentData(studentCsvFilePath);
            InsertClassroomData(classroomCsvFilePath);
            InsertProgrammeData(programmeCsvFilePath);
            InsertCourseData(courseCsvFilePath);
            InsertProgrammeCoursesData(programmeCoursesCsvFilePath);

            MessageBox.Show("CSV data successfully imported into SQL Server!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void InsertStudentData(string csvFilePath)
        {
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    //Replace Connection String
                    using (SqlConnection dbConnection = new SqlConnection(@"Data Source=Short-Feet\SQLEXPRESS; Initial Catalog=dotnet2; Integrated Security=SSPI;"))
                    {
                        dbConnection.Open();

                        while (!csvReader.EndOfData)
                        {
                            string[] fields = csvReader.ReadFields();
                            string query = "INSERT INTO Student (seat_number, program, curr_year, isActive) VALUES (@SeatNumber, @Program, @CurrYear, @IsActive)";
                            using (SqlCommand command = new SqlCommand(query, dbConnection))
                            {
                                command.Parameters.AddWithValue("@SeatNumber", fields[0]);
                                command.Parameters.AddWithValue("@Program", fields[1]);
                                command.Parameters.AddWithValue("@CurrYear", fields[2]);

                                // Convert isActive string to boolean
                                bool isActiveValue = (fields[3].ToLower() == "yes" || fields[3] == "1");
                                command.Parameters.AddWithValue("@IsActive", isActiveValue);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting data from Student CSV into SQL Server: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void InsertClassroomData(string csvFilePath)
        {
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    using (SqlConnection dbConnection = new SqlConnection(@"Data Source=Short-Feet\SQLEXPRESS; Initial Catalog=dotnet2; Integrated Security=SSPI;"))
                    {
                        dbConnection.Open();

                        while (!csvReader.EndOfData)
                        {
                            string[] fields = csvReader.ReadFields();
                            int roomNumber = Convert.ToInt32(fields[0]);
                            int rows = Convert.ToInt32(fields[1]);
                            int cols = Convert.ToInt32(fields[2]);
                            int total = Convert.ToInt32(fields[3]);

                            string query = "INSERT INTO Classroom (room_number, rowss, cols, total) VALUES (@RoomNumber, @Rows, @Cols, @Total)";
                            using (SqlCommand command = new SqlCommand(query, dbConnection))
                            {
                                command.Parameters.AddWithValue("@RoomNumber", roomNumber);
                                command.Parameters.AddWithValue("@Rows", rows);
                                command.Parameters.AddWithValue("@Cols", cols);
                                command.Parameters.AddWithValue("@Total", total);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting data from Classroom CSV into SQL Server: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void InsertProgrammeData(string csvFilePath)
        {
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    using (SqlConnection dbConnection = new SqlConnection(@"Data Source=Short-Feet\SQLEXPRESS; Initial Catalog=dotnet2; Integrated Security=SSPI;"))
                    {
                        dbConnection.Open();

                        while (!csvReader.EndOfData)
                        {
                            string[] fields = csvReader.ReadFields();
                            string query = "INSERT INTO Programme (programme_name) VALUES (@ProgrammeName)";
                            using (SqlCommand command = new SqlCommand(query, dbConnection))
                            {
                                command.Parameters.AddWithValue("@ProgrammeName", fields[0]);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting data from Programme CSV into SQL Server: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void InsertCourseData(string csvFilePath)
        {
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    using (SqlConnection dbConnection = new SqlConnection(@"Data Source=Short-Feet\SQLEXPRESS; Initial Catalog=dotnet2; Integrated Security=SSPI;"))
                    {
                        dbConnection.Open();

                        while (!csvReader.EndOfData)
                        {
                            string[] fields = csvReader.ReadFields();
                            string query = "INSERT INTO Courses (course_name) VALUES (@CourseName)";
                            using (SqlCommand command = new SqlCommand(query, dbConnection))
                            {
                                command.Parameters.AddWithValue("@CourseName", fields[0]);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting data from Courses CSV into SQL Server: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void InsertProgrammeCoursesData(string csvFilePath)
        {
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    using (SqlConnection dbConnection = new SqlConnection(@"Data Source=Short-Feet\SQLEXPRESS; Initial Catalog=dotnet2; Integrated Security=SSPI;"))
                    {
                        dbConnection.Open();

                        while (!csvReader.EndOfData)
                        {
                            string[] fields = csvReader.ReadFields();
                            string query = "INSERT INTO ProgrammeCourses (programme_name, course_name) VALUES (@ProgrammeName, @CourseName)";
                            using (SqlCommand command = new SqlCommand(query, dbConnection))
                            {
                                command.Parameters.AddWithValue("@ProgrammeName", fields[0]);
                                command.Parameters.AddWithValue("@CourseName", fields[1]);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting data from ProgrammeCourses CSV into SQL Server: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Function to Call CSV to DATABASE 
            //Conversion
            CSVToDBDataInsertion();
        }
    }
}
