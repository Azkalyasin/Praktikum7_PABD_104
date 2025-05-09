using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Praktikum7
{
    public partial class Form1: Form
    {
        private readonly string connectionString = "Data Source=LAPTOP-PGU1KG1D\\AZKALADZKIA;Initial Catalog=OrganisasiMahasiswa;Integrated Security=True;";

        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly CacheItemPolicy _policy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5)
        };
        private const string CacheKey = "MahasiswaData";

        public Form1()
        {
            InitializeComponent();
        }

        private void EnsureIndexes()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var indexScript = @"
                IF OBJECT_ID('dbo.Mahasiswa', 'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='idx_Mahasiswa_Nama')
                        CREATE NONCLUSTERED INDEX idx_Mahasiswa_Nama ON dbo.Mahasiswa(Nama);
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='idx_Mahasiswa_Email')
                        CREATE NONCLUSTERED INDEX idx_Mahasiswa_Email ON dbo.Mahasiswa(Email);
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='idx_Mahasiswa_Telpon')
                        CREATE NONCLUSTERED INDEX idx_Mahasiswa_Telpon ON dbo.Mahasiswa(Telpon);
                END";
                using (var cmd = new SqlCommand(indexScript, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }



        private void ClearForm()
        {
            txtNIM.Clear();
            txtNama.Clear();
            txtEmail.Clear();
            txtTelepon.Clear();
            txtAlamat.Clear();
            dgvMahasiswa.ClearSelection();
        }


        private void BtnTambah_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNIM.Text) || string.IsNullOrWhiteSpace(txtNama.Text))
            {
                MessageBox.Show("Harap isi semua data!", "Peringatan!");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("AddMahasiswa", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@NIM", txtNIM.Text.Trim());
                        cmd.Parameters.AddWithValue("@Nama", txtNama.Text.Trim());
                        cmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@Telpon", txtTelepon.Text.Trim());
                        cmd.Parameters.AddWithValue("@Alamat", txtAlamat.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }

                _cache.Remove(CacheKey);
                MessageBox.Show("Data berhasil ditambahkan!", "Sukses");
                LoadData();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex.Message, "Kesalahan");
            }
        }

        private void BtnUbah_Click(object sender, EventArgs e)
        {
            if (dgvMahasiswa.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih data yang akan diubah!", "Peringatan");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("UpdateMahasiswa", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@NIM", txtNIM.Text.Trim());
                        cmd.Parameters.AddWithValue("@Nama", txtNama.Text.Trim());
                        cmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@Telpon", txtTelepon.Text.Trim());
                        cmd.Parameters.AddWithValue("@Alamat", txtAlamat.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }

                _cache.Remove(CacheKey);
                MessageBox.Show("Data berhasil diperbarui!", "Sukses");
                LoadData();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Kesalahan");
            }
        }

        private void AnalyzeQuery(string sqlQuery)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.InfoMessage += (s, e) => MessageBox.Show(e.Message, "STATISTICS INFO");
                conn.Open();
                var wrapped = $@"
        SET STATISTICS IO ON;
        SET STATISTICS TIME ON;
        {sqlQuery};
        SET STATISTICS IO OFF;
        SET STATISTICS TIME OFF;";
                using (var cmd = new SqlCommand(wrapped, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void BtnHapus_Click(object sender, EventArgs e)
        {
            if (dgvMahasiswa.SelectedRows.Count == 0) return;
            if (MessageBox.Show("Yakin ingin menghapus data ini?", "Konfirmasi", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                var nim = dgvMahasiswa.SelectedRows[0].Cells["NIM"].Value.ToString();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("DeleteMahasiswa", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@NIM", nim);
                        cmd.ExecuteNonQuery();
                    }
                }

                _cache.Remove(CacheKey);
                MessageBox.Show("Data berhasil dihapus!", "Sukses");
                LoadData();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Kesalahan");
            }
        }

        private void LoadData()
        {
            DataTable dt;
            if (_cache.Contains(CacheKey))
            {
                dt = _cache.Get(CacheKey) as DataTable;
            }
            else
            {
                dt = new DataTable();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var query = "SELECT NIM AS [NIM], Nama, Email, Telpon, Alamat FROM dbo.Mahasiswa;";
                    var a = new SqlDataAdapter(query, conn);
                    a.Fill(dt);
                }

                _cache.Add(CacheKey, dt, _policy);
            }

            dgvMahasiswa.AutoGenerateColumns = true;
            dgvMahasiswa.DataSource = dt;
        }

        private void BtnImportData_Click(object sender, EventArgs e)
        {
            using (var openFile = new OpenFileDialog())
            {
                openFile.Filter = "Excel Files|*.xlsx;*.xlsm";
                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    PreviewData(openFile.FileName);
                }
            }
        }

        private void PreviewData(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook = new XSSFWorkbook(fs);
                    ISheet sheet = workbook.GetSheetAt(0);
                    DataTable dt = new DataTable();

                    // Header kolom
                    IRow headerRow = sheet.GetRow(0);
                    foreach (var cell in headerRow.Cells)
                    {
                        dt.Columns.Add(cell.ToString());
                    }

                    // Baris data
                    for (int i = 1; i <= sheet.LastRowNum; i++)
                    {
                        IRow dataRow = sheet.GetRow(i);
                        if (dataRow == null) continue;
                        DataRow newRow = dt.NewRow();

                        for (int j = 0; j < dataRow.Cells.Count; j++)
                        {
                            var cell = dataRow.Cells[j];
                            newRow[j] = cell?.ToString() ?? "";
                        }

                        dt.Rows.Add(newRow);
                    }

                    PreviewForm previewForm = new PreviewForm(dt);
                    previewForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading the Excel file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            _cache.Remove(CacheKey);
            LoadData();
        }

        private void dgvMahasiswa_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void dgvMahasiswa_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvMahasiswa.Rows[e.RowIndex];
            txtNIM.Text = row.Cells[0].Value?.ToString() ?? string.Empty;
            txtNama.Text = row.Cells[1].Value?.ToString() ?? string.Empty;
            txtEmail.Text = row.Cells[2].Value?.ToString() ?? string.Empty;
            txtTelepon.Text = row.Cells[3].Value?.ToString() ?? string.Empty;
            txtAlamat.Text = row.Cells[4].Value?.ToString() ?? string.Empty;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();
            EnsureIndexes();
        }


        private void Analisis_Click(object sender, EventArgs e)
        {
            var heavyQuery = "SELECT Nama, Email, Telpon FROM dbo.Mahasiswa WHERE Nama LIKE 'A%'";
            AnalyzeQuery(heavyQuery);
        }
    }
}
