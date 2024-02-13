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
using Org.BouncyCastle.Utilities.Collections;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.VisualBasic.Devices;
using static iTextSharp.text.pdf.events.IndexEvents;


namespace Exam_Seating_Arrangment
{
    public partial class Form1 : Form
    {
        // Connection string for SQL Server
        string connectionString = "Data Source=TARUNJOSHI\\SQLEXPRESS;Initial Catalog=ExaminationCell;Integrated Security=True";
        Dictionary<string, List<List<(long, string)>>> classrooms;
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Check if the row is not a new row (this might be present at the end)
                if (!row.IsNewRow)
                {
                    // Get the value of the cell in the third column (index 2, as indexing is zero-based)
                    object cellValue = row.Cells[3].Value;

                    // Check if the cell value is not null and equals "baf"
                    if (cellValue != null && cellValue.ToString() == textBox1.Text.ToUpper())
                    {
                        // Perform actions with the row where the forth column's value is TextBox's input
                        // Output row values or perform other actions as needed
                    }
                }
            }
        }

        private Dictionary<string, List<List<(long, string)>>> getClassRoomDataFromDataGridView()
        {
            Dictionary<string, List<List<(long, string)>>> classrooms = new Dictionary<string, List<List<(long, string)>>>();
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                // Retrieve data from dataGridView2
                string roomNumber = row.Cells["RoomNumberColumn"].Value?.ToString();
                string capacity = row.Cells["CapacityColumn"].Value?.ToString();

                // Check if roomNumber and capacity are not null or empty
                if (!string.IsNullOrEmpty(roomNumber) && !string.IsNullOrEmpty(capacity))
                {
                    // Convert capacity to long
                    if (long.TryParse(capacity, out long capacityValue))
                    {
                        string classroomNumber = roomNumber;
                        long loopCondition = Convert.ToInt64(capacity);
                        loopCondition /= 2;
                        //MessageBox.Show(classroomNumber);
                        classrooms[classroomNumber] = new List<List<(long, string)>>();
                        for (int i = 0; i < loopCondition; i++)
                        {
                            classrooms[classroomNumber].Add(new List<(long, string)>());
                        }
                    }
                    else
                    {
                        // Handle invalid capacity
                        // You may display an error message or take appropriate action
                    }
                }
                else
                {
                    // Handle missing room number or capacity
                    // You may display an error message or take appropriate action
                }
            }
            return classrooms;
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
        // Define a DataTable at class level to hold combined results
        private DataTable combinedDataTable = new DataTable();
        private void CountStudentsByDetails(SqlConnection con)
        {
            string programFilter = textBox2.Text;
            string query = "SELECT MIN(seat_number) AS FromSeat, MAX(seat_number) AS ToSeat, COUNT(*) AS CourseCount, program AS Program, curr_year AS CurrYear, COUNT(assigned) AS UnAssign FROM Student WHERE program = @Program AND assigned = 0 GROUP BY program, curr_year;";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Program", programFilter);
            SqlDataAdapter ad = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            ad.Fill(dt);

            //Merge the new DataTable with the combinedDataTable
            combinedDataTable.Merge(dt);
            dataGridView1.DataSource = combinedDataTable; // Assign combinedDataTable to the DataGridView
        }

        Boolean flag = true;
        private void AddRoom()
        {
            createColumn();
            string roomNumber = txtRoomNumber.Text;
            int Capacity;
            if (string.IsNullOrEmpty(roomNumber))
            {
                MessageBox.Show("Please Enter Room Number. e.g. 501");
                return;
            }
            else
            {
                if (!int.TryParse(txtCapacity.Text, out Capacity))
                {
                    MessageBox.Show("Invalid capacity. Please enter a valid number.");
                    return;
                }
            }

            // Add data to dataGridView2;
            dataGridView2.Rows.Add(roomNumber, Capacity);

            // Clear textboxes after adding data
            txtRoomNumber.Clear();
            txtCapacity.Clear();
            classrooms = getClassRoomDataFromDataGridView();
        }

        private void createColumn()
        {
            if (flag)
            {
                dataGridView2.Columns.Add("RoomNumberColumn", "Room Number");
                dataGridView2.Columns.Add("CapacityColumn", "Capacity");
                flag = false;
            }
        }
        private void AssignStudents(SqlConnection con)
        {
            List<long> AssignList = new List<long>();
            string[] programFilters = getSelectedProgramsFromDataGridView();

            // Create a parameterized SQL query string with the appropriate number of parameters
            string query = $"SELECT * FROM Student WHERE program IN ({string.Join(",", programFilters.Select((_, i) => $"@Program{i}"))})";

            /*string programFilter = textBox2.Text;
            string query = "Select * from Student where program = @Program";*/
            using (SqlCommand command = new SqlCommand(query, con))
            {

                for (int i = 0; i < programFilters.Length; i++)
                {
                    command.Parameters.AddWithValue($"@Program{i}", programFilters[i]);
                }

                //command.Parameters.AddWithValue("@Program", programFilter);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    //Console.WriteLine("Roll Number\tCourse");
                    //Console.WriteLine("------------------------");
                    while (reader.Read())
                    {
                        long rollNumber = Convert.ToInt64(reader.GetString(0));
                        string course = reader.GetString(1);
                        bool assigned = reader.GetBoolean(4);

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
                            {
                                AssignList.Add(rollNumber);
                                break;
                            }
                        }
                        if (!assigned)
                        {
                            //Instead of adding it in document i am first storing them in dictionary
                            Dictionary<long, string> UnAssigned = new Dictionary<long, string>();
                            UnAssigned.Add(rollNumber, course);
                            //document.Add(new Paragraph($"Unable to assign student {rollNumber} of {course}"));
                        }

                    }
                }

                using (SqlCommand updateCmd = new SqlCommand("UPDATE Student SET assigned = 1 WHERE seat_number = @RollNumber", con))
                {
                    foreach (long rollNumber in AssignList)
                    {
                        // Clear the parameters collection before adding new parameters
                        updateCmd.Parameters.Clear();

                        // Add the parameter for the current roll number
                        updateCmd.Parameters.AddWithValue("@RollNumber", rollNumber);

                        // Execute the update command
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }
        Dictionary<string, string> blockNumber = new Dictionary<string, string>();
        private void AssignStudents(SqlConnection con, string program)
        {
            List<long> AssignList = new List<long>();

            // Create a parameterized SQL query string with the appropriate number of parameters
            string query = $"SELECT * FROM Student WHERE program = @Program";

            using (SqlCommand command = new SqlCommand(query, con))
            {
                command.Parameters.AddWithValue("@Program", program);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long rollNumber = Convert.ToInt64(reader.GetString(0));
                        string course = reader.GetString(1);
                        bool assigned = reader.GetBoolean(4);
                        if (assigned == false)
                        {
                            foreach (var classroom in classrooms)
                            {
                                if (classroom.Key == textBox1.Text)
                                {
                                    if (!blockNumber.ContainsKey(classroom.Key))
                                    {
                                        blockNumber.Add(classroom.Key, program);
                                    }
                                    foreach (var bench in classroom.Value)
                                    {
                                        if (bench.Count < 2)
                                        {
                                            //Console.WriteLine(CompareWithBenchsFirstStudent(course, bench));
                                            var studentTuple = (rollNumber, course);
                                            bench.Add(studentTuple);
                                            assigned = true;
                                            break;
                                        }
                                    }
                                    if (assigned)
                                    {
                                        AssignList.Add(rollNumber);
                                        break;
                                    }
                                }
                            }
                            if (!assigned)
                            {
                                //Instead of adding it in document i am first storing them in dictionary
                                Dictionary<long, string> UnAssigned = new Dictionary<long, string>();
                                UnAssigned.Add(rollNumber, course);
                                //document.Add(new Paragraph($"Unable to assign student {rollNumber} of {course}"));
                            }
                        }
                    }
                }

                using (SqlCommand updateCmd = new SqlCommand("UPDATE Student SET assigned = 1 WHERE seat_number = @RollNumber", con))
                {
                    foreach (long rollNumber in AssignList)
                    {
                        // Clear the parameters collection before adding new parameters
                        updateCmd.Parameters.Clear();
                        // Add the parameter for the current roll number
                        updateCmd.Parameters.AddWithValue("@RollNumber", rollNumber);
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private string[] getSelectedProgramsFromDataGridView()
        {
            string[] programFilters = new string[dataGridView1.RowCount - 1];
            int count = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Retrieve data from dataGridView2
                string selectedProgram = row.Cells["Program"].Value?.ToString();

                // Check if roomNumber and capacity are not null or empty
                if (!string.IsNullOrEmpty(selectedProgram))
                {
                    //MessageBox.Show(selectedProgram);
                    programFilters[count] = selectedProgram;
                    count++;

                }
                else
                {
                    // Handle missing room number or capacity
                    // You may display an error message or take appropriate action
                }
            }

            for (int i = 0; i < programFilters.Count(); i++)
            {
                //MessageBox.Show(i.ToString());
                //MessageBox.Show(programFilters[i]);
            }

            return programFilters;
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
            string studentCsvFilePath = @"C:\Users\tarun\Downloads\csvfiles\eg - Sheet1.csv";
            //string classroomCsvFilePath = @"C:\Users\tarun\Downloads\csvfiles\eg - Sheet3.csv";
            string programmeCsvFilePath = @"C:\Users\tarun\Downloads\csvfiles\eg - Sheet2.csv";
            string courseCsvFilePath = @"C:\Users\tarun\Downloads\csvfiles\eg - Sheet4.csv";
            string programmeCoursesCsvFilePath = @"C:\Users\tarun\Downloads\csvfiles\eg - Sheet5.csv";

            // Insert data from CSV files into respective tables
            InsertStudentData(studentCsvFilePath);
            //InsertClassroomData(classroomCsvFilePath);
            InsertProgrammeData(programmeCsvFilePath);
            InsertCourseData(courseCsvFilePath);
            InsertProgrammeCoursesData(programmeCoursesCsvFilePath);

            MessageBox.Show("CSV data successfully imported into SQL Server!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InsertProgrammeCoursesData(string csvFilePath)
        {
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    using (SqlConnection dbConnection = new SqlConnection(connectionString))
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

        private void InsertStudentData(string csvFilePath)
        {
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    //Replace Connection String
                    using (SqlConnection dbConnection = new SqlConnection(connectionString))
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

                    using (SqlConnection dbConnection = new SqlConnection(connectionString))
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

                    using (SqlConnection dbConnection = new SqlConnection(connectionString))
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

                    using (SqlConnection dbConnection = new SqlConnection(connectionString))
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
        List<string> SelectedProgram = new List<string>();
        private void GetStudentCount_Click(object sender, EventArgs e)
        {
            SelectedProgram.Add(textBox2.Text);
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            CountStudentsByDetails(con);
        }

        private void AddClassRoom_Click(object sender, EventArgs e)
        {
            AddRoom();
        }

        private void insertCSVtoDB_Click(object sender, EventArgs e)
        {
            //Function to Call CSV to DATABASE CONVERSION
            CSVToDBDataInsertion();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Create the classrooms dictionary
                    classrooms = getClassRoomDataFromDataGridView();
                    // Populate the classrooms dictionary based on the fetched data
                    /*foreach (var kvp in classroomData)
                    {
                        string classroomNumber = kvp.Key;
                        int loopCondition = kvp.Value;
                        classrooms[classroomNumber] = new List<List<(long, string)>>();
                        for (int i = 0; i < loopCondition; i++)
                        {
                            classrooms[classroomNumber].Add(new List<(long, string)>());
                        }
                    }*/
                    AssignStudents(con);
                    con.Close();
                }
                MessageBox.Show("Data successfully inserted into database.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void assign_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    AssignStudents(con, textBox4.Text);
                    CountUnAssignStudentsByDetails(con);
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
        private DataTable unAssignDataTable = new DataTable();
        private void CountUnAssignStudentsByDetails(SqlConnection con)
        {
            SqlDataAdapter ad;
            DataTable dt = new DataTable();
            foreach (string Program in SelectedProgram)
            {
                string query = "SELECT MIN(seat_number) AS FromSeat, MAX(seat_number) AS ToSeat, COUNT(*) AS CourseCount, program AS Program, curr_year AS CurrYear, COUNT(assigned) AS UnAssign FROM Student WHERE program = @Program AND assigned = 0 GROUP BY program, curr_year;";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Program", Program);

                ad = new SqlDataAdapter(cmd);
                ad.Fill(dt);

                /*// Merge the new DataTable with the unAssignDataTable
                unAssignDataTable.Merge(dt);*/
            }

            // Assign unAssignDataTable to the DataGridView
            dataGridView1.DataSource = dt;
        }


        private void done_Click(object sender, EventArgs e)
        {
            PrintDataIntoPDF();
            MakeStudentsUnAssign();
        }

        private void MakeStudentsUnAssign()
        {
            try
            {
                SqlConnection con = new SqlConnection(connectionString);
                con.Open();
                string query = "UPDATE Student SET assigned = 0;";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("hELLO " + ex.Message);
            }

        }

        Boolean first = true;
        private void PrintDataIntoPDF()
        {
            FileMode fm;
            if (first)
            {
                fm = FileMode.Create;
                first = false;
            }
            else
            {
                fm = FileMode.Append;
            }
            using (FileStream fs = new FileStream("C://Tarun_java//seating.pdf", fm))
            {
                Document document = new Document();
                PdfWriter.GetInstance(document, fs);
                document.Open();

                Dictionary<string, long> startingRollNumbers = new Dictionary<string, long>();
                Dictionary<string, long> lastRollNumbers = new Dictionary<string, long>();
                /*foreach (var classroom in classrooms)
                {
                    foreach (var bench in classroom.Value)
                    {
                        foreach (var student in bench)
                        {
                            string subject = student.Item2;
                            long rollNumber = student.Item1;

                            if (!startingRollNumbers.ContainsKey(subject + classroom.Key))
                            {
                                startingRollNumbers.Add(subject + classroom.Key, rollNumber);
                            }
                            else
                            {
                                startingRollNumbers[subject + classroom.Key] = Math.Min(startingRollNumbers[subject + classroom.Key], rollNumber);
                            }

                            // Update last roll number for each subject
                            if (!lastRollNumbers.ContainsKey(subject + classroom.Key))
                            {
                                lastRollNumbers.Add(subject + classroom.Key, rollNumber);
                            }
                            else
                            {
                                lastRollNumbers[subject + classroom.Key] = Math.Max(lastRollNumbers[subject + classroom.Key], rollNumber);
                            }
                        }
                    }
                }*/
                /*foreach (var classroom in classrooms)
                {
                    foreach (var b in blockNumber)
                    {
                        if (classroom.Key == b.Key)
                        {
                            foreach (var entry in startingRollNumbers)
                            {
                                if ((b.Value+b.Key).ToUpper() == entry.Key.ToUpper())
                                {
                                    MessageBox.Show(b.Value.ToUpper() + entry.Key.ToUpper());
                                    document.Add(new Paragraph("Room No: " + classroom.Key));
                                    document.Add(new Paragraph($"Block: {b.Key+b.Value},Subject: {entry.Key}, Starting Roll Number: {entry.Value}, Last Roll Number: {lastRollNumbers[entry.Key]}"));
                                    foreach (var bench in classroom.Value)
                                    {
                                        document.Add(new Paragraph("Bench:"));
                                        foreach (var student in bench)
                                        {
                                            document.Add(new Paragraph($"  {student.Item1}: {student.Item2}"));
                                        }
                                    }
                                }
                            }
                        } 
                    }
                }*/
                foreach (var classroom in classrooms)
                {
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
                }
                foreach (var b in blockNumber)
                {
                    foreach(var entry in startingRollNumbers)
                    {
                        if(b.Value.ToUpper() == entry.Key.ToUpper())
                        {
                            document.Add(new Paragraph("Room No: " + b.Key));
                            document.Add(new Paragraph($"Block: ,Subject: {entry.Key}, Starting Roll Number: {entry.Value}, Last Roll Number: {lastRollNumbers[entry.Key]}"));
                        }
                    }
                }

                //I will close document when user click finished
                document.Close();
            }
        }
    }
}
