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
    public partial class FormOrganisasi: Form
    {
        static string connectionString = "Data Source=LAPTOP-PGU1KG1D\\AZKALADZKIA;Initial Catalog=OrganisasiMahasiswa;Integrated Security=True;";
        private int selectedIdOrganisasi = -1;

        public FormOrganisasi()
        {
            InitializeComponent();
            this.BackColor = Color.MediumPurple;
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            string namaOrganisasi = txtNamaOrganisasi.Text;
            string deskripsi = txtDeskripsi.Text;
            int tahunBerdiri;
            decimal jumlah;
            string keterangan = txtKeterangan.Text;

            // Validasi input
            if (string.IsNullOrEmpty(namaOrganisasi) || string.IsNullOrEmpty(deskripsi) ||
                !int.TryParse(txtTahunBerdiri.Text, out tahunBerdiri) ||
                !decimal.TryParse(txtJumlah.Text, out jumlah) || string.IsNullOrEmpty(keterangan))
            {
                lblMessage.Text = "Isi kolom dengan data yang sesuai";
                return;
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlTransaction transaction = null;

                try
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    SqlCommand cmd = new SqlCommand
                    {
                        Connection = conn,
                        Transaction = transaction
                    };

                    // Insert into Organisasi table
                    cmd.CommandText = "INSERT INTO Organisasi (NamaOrganisasi, Deskripsi, TahunBerdiri) OUTPUT INSERTED.ID_Organisasi VALUES (@NamaOrganisasi, @Deskripsi, @TahunBerdiri)";
                    cmd.Parameters.AddWithValue("@NamaOrganisasi", namaOrganisasi);
                    cmd.Parameters.AddWithValue("@Deskripsi", deskripsi);
                    cmd.Parameters.AddWithValue("@TahunBerdiri", tahunBerdiri);

                    object result = cmd.ExecuteScalar();
                    if (result == DBNull.Value)
                    {
                        lblMessage.Text = "Gagal mengambil ID Organisasi";
                        return;
                    }

                    int idOrganisasi = Convert.ToInt32(result);

                    // Insert into Keuangan table
                    cmd.CommandText = "INSERT INTO Keuangan (ID_Organisasi, Jenis, Jumlah, Tanggal, Keterangan) VALUES (@ID_Organisasi, 'Pemasukan', @Jumlah, GETDATE(), @Keterangan)";
                    cmd.Parameters.AddWithValue("@ID_Organisasi", idOrganisasi);
                    cmd.Parameters.AddWithValue("@Jumlah", jumlah);
                    cmd.Parameters.AddWithValue("@Keterangan", keterangan);
                    cmd.ExecuteNonQuery();

                    transaction.Commit();
                    lblMessage.Text = "Data berhasil disimpan";
                    LoadJoinedData(); // Reload data after insert
                }
                catch (Exception ex)
                {
                    transaction?.Rollback();
                    lblMessage.Text = "Error: " + ex.Message;
                }
            }
        }

        private void LoadJoinedData()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
        SELECT 
            Organisasi.ID_Organisasi,
            Organisasi.NamaOrganisasi,
            Organisasi.Deskripsi,
            Organisasi.TahunBerdiri,
            Keuangan.Jumlah,
            Keuangan.Tanggal,
            Keuangan.Keterangan
        FROM 
            Organisasi
        INNER JOIN 
            Keuangan ON Organisasi.ID_Organisasi = Keuangan.ID_Organisasi";

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, conn);
                DataTable dataTable = new DataTable();

                try
                {
                    dataAdapter.Fill(dataTable);
                    dataGridViewOrganisasi.DataSource = dataTable; // Menampilkan hasil gabungan di DataGridView

                    // Menyembunyikan kolom ID_Organisasi dari tampilan DataGridView
                    dataGridViewOrganisasi.Columns["ID_Organisasi"].Visible = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading data: " + ex.Message);
                }
            }
        }

        private void dataGridViewOrganisasi_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Pastikan indeks baris valid
            if (e.RowIndex >= 0 && dataGridViewOrganisasi.Rows.Count > 0)
            {
                try
                {
                    // Ambil ID_Organisasi dari baris yang dipilih
                    var idOrganisasiCellValue = dataGridViewOrganisasi.Rows[e.RowIndex].Cells["ID_Organisasi"].Value;
                    if (idOrganisasiCellValue != null && idOrganisasiCellValue != DBNull.Value)
                    {
                        // Simpan ID yang dipilih untuk digunakan nanti (pada operasi update/delete)
                        selectedIdOrganisasi = Convert.ToInt32(idOrganisasiCellValue);

                        // Isi TextBox dengan data dari baris yang dipilih
                        txtNamaOrganisasi.Text = dataGridViewOrganisasi.Rows[e.RowIndex].Cells["NamaOrganisasi"].Value?.ToString() ?? "";
                        txtDeskripsi.Text = dataGridViewOrganisasi.Rows[e.RowIndex].Cells["Deskripsi"].Value?.ToString() ?? "";
                        txtTahunBerdiri.Text = dataGridViewOrganisasi.Rows[e.RowIndex].Cells["TahunBerdiri"].Value?.ToString() ?? "";

                        // Mengambil nilai dari kolom "Jumlah"
                        var jumlahValue = dataGridViewOrganisasi.Rows[e.RowIndex].Cells["Jumlah"].Value;

                        // Cek apakah jumlahValue tidak null dan bukan DBNull
                        if (jumlahValue != null && jumlahValue != DBNull.Value)
                        {
                            // Jika ada nilai yang valid, tampilkan
                            txtJumlah.Text = jumlahValue.ToString();
                        }
                        else
                        {
                            // Set text box ke kosong jika null atau DBNull
                            txtJumlah.Text = "";
                        }

                        // Cek dan isi keterangan
                        txtKeterangan.Text = dataGridViewOrganisasi.Rows[e.RowIndex].Cells["Keterangan"].Value?.ToString() ?? "";
                    }
                    else
                    {
                        MessageBox.Show("ID organisasi tidak valid.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saat mengambil data: " + ex.Message);
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedIdOrganisasi == -1)
            {
                lblMessage.Text = "Pilih nama organisasi yang ingin dihapus";
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlTransaction transaction = null;

                try
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    SqlCommand cmd = new SqlCommand
                    {
                        Connection = conn,
                        Transaction = transaction
                    };

                    // Delete from Keuangan table
                    cmd.CommandText = "DELETE FROM Keuangan WHERE ID_Organisasi = @ID_Organisasi";
                    cmd.Parameters.AddWithValue("@ID_Organisasi", selectedIdOrganisasi);
                    cmd.ExecuteNonQuery();

                    // Delete from Organisasi table
                    cmd.CommandText = "DELETE FROM Organisasi WHERE ID_Organisasi = @ID_Organisasi";
                    cmd.ExecuteNonQuery();

                    transaction.Commit();
                    lblMessage.Text = "Data berhasil dihapus.";
                    LoadJoinedData(); // Reload DataGridView after delete
                }
                catch (Exception ex)
                {
                    transaction?.Rollback();
                    lblMessage.Text = "Error: " + ex.Message;
                }
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedIdOrganisasi == -1)
            {
                lblMessage.Text = "Pilih nama organisasi yang ingin diubah";
                return;
            }

            // Ambil data dari textboxs untuk update
            string namaOrganisasi = txtNamaOrganisasi.Text;
            string deskripsi = txtDeskripsi.Text;
            int tahunBerdiri;
            decimal jumlah;
            string keterangan = txtKeterangan.Text;

            // Validasi input
            if (string.IsNullOrEmpty(namaOrganisasi) || string.IsNullOrEmpty(deskripsi) ||
                !int.TryParse(txtTahunBerdiri.Text, out tahunBerdiri) ||
                !decimal.TryParse(txtJumlah.Text, out jumlah) || string.IsNullOrEmpty(keterangan))
            {
                lblMessage.Text = "Isi kolom dengan data yang sesuai!";
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlTransaction transaction = null;

                try
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    SqlCommand cmd = new SqlCommand
                    {
                        Connection = conn,
                        Transaction = transaction
                    };

                    cmd.CommandText = "UPDATE Organisasi SET NamaOrganisasi = @NamaOrganisasi, Deskripsi = @Deskripsi, TahunBerdiri = @TahunBerdiri WHERE ID_Organisasi = @ID_Organisasi";
                    cmd.Parameters.AddWithValue("@NamaOrganisasi", namaOrganisasi);
                    cmd.Parameters.AddWithValue("@Deskripsi", deskripsi);
                    cmd.Parameters.AddWithValue("@TahunBerdiri", tahunBerdiri);
                    cmd.Parameters.AddWithValue("@ID_Organisasi", selectedIdOrganisasi);
                    cmd.ExecuteNonQuery();

                    // Update Keuangan table
                    cmd.CommandText = "UPDATE Keuangan SET Jumlah = @Jumlah, Keterangan = @Keterangan WHERE ID_Organisasi = @ID_Organisasi";
                    cmd.Parameters.AddWithValue("@Jumlah", jumlah);
                    cmd.Parameters.AddWithValue("@Keterangan", keterangan);
                    cmd.ExecuteNonQuery();

                    transaction.Commit();
                    lblMessage.Text = "Data berhasil diubah.";
                    LoadJoinedData();
                }
                catch (Exception ex)
                {
                    transaction?.Rollback();
                    lblMessage.Text = "Error: " + ex.Message;
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            selectedIdOrganisasi = -1;
            txtNamaOrganisasi.Clear();
            txtDeskripsi.Clear();
            txtTahunBerdiri.Clear();
            txtJumlah.Clear();
            txtKeterangan.Clear();
            lblMessage.Text = "";
            LoadJoinedData();
        }
    }
}
