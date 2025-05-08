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

namespace Praktikum7
{
    public partial class PreviewForm: Form
    {
        static string connectionString = "Data Source=LAPTOP-PGU1KG1D\\AZKALADZKIA;Initial Catalog=OrganisasiMahasiswa;Integrated Security=True;";
        public PreviewForm(DataTable dt)
        {
            InitializeComponent();
            dgvPreview.DataSource = dt; 
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Menampilkan pesan konfirmasi apakah ingin mengimpor data
            DialogResult result = MessageBox.Show("Apakah anda ingin mengimpor data ini ke database?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Mengimpor data dari DataGridView ke database
                ImportDataToDatabase();
            }
        }

        private bool ValidateRow(DataRow row)
        {
            string nim = row["NIM"].ToString();

            // Validasi NIM (misalnya, harus berjumlah 11 karakter)
            if (nim.Length != 11)
            {
                MessageBox.Show("NIM harus terdiri dari 11 karakter.", "Kesalahan Validasi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Jika perlu, tambahkan validasi lain sesuai dengan kebutuhan (misalnya, pola tertentu untuk NIM)

            return true;
        }

        private void ImportDataToDatabase()
        {
            try
            {
                DataTable dt = (DataTable)dgvPreview.DataSource;

                foreach (DataRow row in dt.Rows)
                {
                    // Validasi setiap baris sebelum diimpor
                    if (!ValidateRow(row))
                    {
                        // Jika validasi gagal, lanjutkan ke baris berikutnya
                        continue; // Lewati baris ini jika tidak valid
                    }

                    string query = "INSERT INTO Mahasiswa (NIM, Nama, Email, Telpon, Alamat) VALUES (@NIM, @Nama, @Email, @Telpon, @Alamat)";
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@NIM", row["NIM"]);
                            cmd.Parameters.AddWithValue("@Nama", row["Nama"]);
                            cmd.Parameters.AddWithValue("@Email", row["Email"]);
                            cmd.Parameters.AddWithValue("@Telpon", row["Telpon"]);
                            cmd.Parameters.AddWithValue("@Alamat", row["Alamat"]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Data berhasil diimpor ke database.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close(); // Tutup PreviewForm setelah data diimpor
            }
            catch (Exception ex)
            {
                MessageBox.Show("Terjadi kesalahan saat mengimpor data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PreviewForm_Load(object sender, EventArgs e)
        {
            dgvPreview.AutoResizeColumns();
        }
    }
}

