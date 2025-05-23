# Rencana Pengujian Sistem Pathfinding

Dokumen ini menguraikan pendekatan pengujian komprehensif untuk sistem pathfinding, termasuk pengujian komponen sistem dan pengujian kinerja algoritma.

## I. Pengujian Komponen Sistem

### 1. Pengujian Input Ukuran Grid

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| GS-01 | Memasukkan ukuran grid yang valid (mis., 20x20) | Grid diubah ukuran dengan benar | □ |
| GS-02 | Memasukkan ukuran grid maksimum (200x200) | Grid diubah ukuran ke ukuran maksimum | □ |
| GS-03 | Memasukkan nilai melebihi maksimum (mis., 201x201) | Operasi ditolak, peringatan ditampilkan | □ |
| GS-04 | Memasukkan nilai non-numerik | Input ditolak | □ |
| GS-05 | Memasukkan nilai negatif atau nol | Input ditolak | □ |
| GS-06 | Mengubah ukuran selama pathfinding | Operasi diblokir selama pathfinding | □ |
| GS-07 | Menguji grid tidak persegi (mis., 20x30) | Grid membuat bentuk persegi panjang yang benar | □ |

### 2. Pengujian Dropdown Algoritma

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| ALG-01 | Pilih A* | Algoritma berubah ke A* | □ |
| ALG-02 | Pilih Dijkstra | Algoritma berubah ke Dijkstra | □ |
| ALG-03 | Pilih Greedy | Algoritma berubah ke Greedy | □ |
| ALG-04 | Pilih Backtracking | Algoritma berubah ke Backtracking | □ |
| ALG-05 | Pilih BFS | Algoritma berubah ke BFS | □ |
| ALG-06 | Mengubah algoritma selama pathfinding | Operasi diblokir selama pathfinding | □ |

### 3. Pengujian Toggle Pergerakan Diagonal

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| DIAG-01 | Aktifkan pergerakan diagonal | Pergerakan diagonal diaktifkan, jalur dapat menggunakan diagonal | □ |
| DIAG-02 | Nonaktifkan pergerakan diagonal | Pergerakan diagonal dinonaktifkan, jalur hanya menggunakan arah kardinal | □ |
| DIAG-03 | Toggle selama pathfinding | Operasi diblokir selama pathfinding | □ |

### 4. Pengujian Kontrol Visualisasi

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| VIS-01 | Menyesuaikan kecepatan visualisasi (meningkat) | Visualisasi berjalan lebih cepat | □ |
| VIS-02 | Menyesuaikan kecepatan visualisasi (menurun) | Visualisasi berjalan lebih lambat | □ |
| VIS-03 | Mengatur ukuran batch ke 1 | Setiap langkah divisualisasikan secara individual | □ |
| VIS-04 | Mengatur ukuran batch ke nilai lebih tinggi | Beberapa langkah divisualisasikan bersama | □ |
| VIS-05 | Menyesuaikan kontrol selama pathfinding | Operasi diblokir selama pathfinding | □ |

### 5. Pengujian Generator Labirin

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| MAZE-01 | Menghasilkan labirin kecil (kepadatan rendah) | Labirin dihasilkan dengan sedikit rintangan | □ |
| MAZE-02 | Menghasilkan labirin sedang (kepadatan sedang) | Labirin dihasilkan dengan rintangan moderat | □ |
| MAZE-03 | Menghasilkan labirin besar (kepadatan tinggi) | Labirin dihasilkan dengan banyak rintangan | □ |
| MAZE-04 | Menghasilkan labirin dengan kepadatan 0% | Grid kosong sepenuhnya dihasilkan | □ |
| MAZE-05 | Menghasilkan labirin dengan kepadatan 100% | Grid terisi penuh dihasilkan | □ |
| MAZE-06 | Menghasilkan labirin selama pathfinding | Operasi diblokir selama pathfinding | □ |

### 6. Pengujian Simpan dan Muat

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| SAVE-01 | Simpan dengan nama file valid | Labirin berhasil disimpan | □ |
| SAVE-02 | Simpan dengan nama file kosong | Pesan kesalahan ditampilkan | □ |
| SAVE-03 | Simpan selama pathfinding | Operasi diblokir selama pathfinding | □ |
| LOAD-01 | Muat file yang ada | Labirin berhasil dimuat | □ |
| LOAD-02 | Muat dengan nama file yang tidak ada | Pesan kesalahan ditampilkan | □ |
| LOAD-03 | Muat selama pathfinding | Operasi diblokir selama pathfinding | □ |

### 7. Pengujian Tombol Muat Ulang

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| RELOAD-01 | Klik tombol muat ulang | Scene dimuat ulang ke keadaan awal | □ |
| RELOAD-02 | Klik muat ulang selama pathfinding | Scene dimuat ulang, operasi diizinkan selama pathfinding | □ |
| RELOAD-03 | Klik muat ulang setelah pathfinding dalam mode build | Tombol diaktifkan kembali setelah reset | □ |

### 8. Pengujian Status UI

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| UI-01 | Mulai pathfinding | Semua tombol dinonaktifkan kecuali reload dan exit | □ |
| UI-02 | Setelah pathfinding selesai (editor) | Tombol diaktifkan kembali | □ |
| UI-03 | Setelah pathfinding selesai (build) | Tombol tetap dinonaktifkan | □ |
| UI-04 | Interaksi mouse selama pathfinding | Repositioning NPC/tujuan diblokir | □ |

## II. Pengujian Kinerja Algoritma

### 1. Pengujian Algoritma A*

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| ASTAR-01 | Grid kecil (20x20), kepadatan rendah | Jalur ditemukan secara efisien | □ |
| ASTAR-02 | Grid sedang (50x50), kepadatan sedang | Jalur ditemukan dengan kinerja wajar | □ |
| ASTAR-03 | Grid besar (100x100), kepadatan tinggi | Jalur ditemukan tanpa waktu/memori berlebihan | □ |
| ASTAR-04 | Jalur mustahil (kepadatan 100%) | Dengan benar melaporkan tidak ada jalur yang ditemukan | □ |
| ASTAR-05 | Dengan pergerakan diagonal | Jalur diagonal yang lebih pendek digunakan | □ |
| ASTAR-06 | Tanpa pergerakan diagonal | Hanya jalur kardinal yang digunakan | □ |

### 2. Pengujian Algoritma Dijkstra

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| DIJK-01 | Grid kecil (20x20), kepadatan rendah | Jalur ditemukan dengan eksplorasi lebih dari A* | □ |
| DIJK-02 | Grid sedang (50x50), kepadatan sedang | Jalur ditemukan dengan waktu/memori lebih tinggi dari A* | □ |
| DIJK-03 | Grid besar (100x100), kepadatan tinggi | Jalur ditemukan, mungkin dengan penggunaan sumber daya tinggi | □ |
| DIJK-04 | Jalur mustahil (kepadatan 100%) | Dengan benar melaporkan tidak ada jalur yang ditemukan | □ |
| DIJK-05 | Dengan pergerakan diagonal | Jalur optimal ditemukan | □ |
| DIJK-06 | Tanpa pergerakan diagonal | Jalur kardinal optimal ditemukan | □ |

### 3. Pengujian Greedy Best-First Search

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| GREEDY-01 | Grid kecil (20x20), kepadatan rendah | Kinerja cepat, jalur berpotensi tidak optimal | □ |
| GREEDY-02 | Grid sedang (50x50), kepadatan sedang | Kinerja cepat dengan memori lebih sedikit dari A*/Dijkstra | □ |
| GREEDY-03 | Grid besar (100x100), kepadatan tinggi | Lebih cepat dari A*/Dijkstra, tetapi mungkin tidak optimal | □ |
| GREEDY-04 | Jalur mustahil (kepadatan 100%) | Dengan benar melaporkan tidak ada jalur yang ditemukan | □ |
| GREEDY-05 | Labirin dengan bottleneck | Mungkin menghasilkan jalur yang tidak optimal | □ |

### 4. Pengujian Algoritma Backtracking

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| BACK-01 | Grid kecil (20x20), kepadatan rendah | Jalur ditemukan, kemungkinan lebih lambat dari algoritma lain | □ |
| BACK-02 | Grid sedang (50x50), kepadatan sedang | Degradasi kinerja dengan ukuran meningkat | □ |
| BACK-03 | Labirin kecil dengan kepadatan tinggi | Mungkin kesulitan dengan labirin kompleks | □ |
| BACK-04 | Jalur mustahil (kepadatan 100%) | Dengan benar melaporkan tidak ada jalur yang ditemukan | □ |

### 5. Pengujian Algoritma BFS

| ID Tes | Kasus Uji | Hasil yang Diharapkan | Lulus/Gagal |
|---------|-----------|----------------|-----------|
| BFS-01 | Grid kecil (20x20), kepadatan rendah | Jalur optimal untuk grid tanpa bobot | □ |
| BFS-02 | Grid sedang (50x50), kepadatan sedang | Penggunaan memori lebih tinggi dari A* | □ |
| BFS-03 | Grid besar (100x100), kepadatan tinggi | Konsumsi memori tinggi | □ |
| BFS-04 | Jalur mustahil (kepadatan 100%) | Dengan benar melaporkan tidak ada jalur yang ditemukan | □ |

## III. Matriks Kinerja Sistem Keseluruhan

| Algoritma | Grid Kecil | Grid Sedang | Grid Besar | Kepadatan Rendah | Kepadatan Tinggi | Dengan Diagonal | Tanpa Diagonal |
|-----------|------------|-------------|------------|------------------|-----------------|----------------|----------------|
| A* | □ | □ | □ | □ | □ | □ | □ |
| Dijkstra | □ | □ | □ | □ | □ | □ | □ |
| Greedy | □ | □ | □ | □ | □ | □ | □ |
| Backtracking | □ | □ | □ | □ | □ | □ | □ |
| BFS | □ | □ | □ | □ | □ | □ | □ |

## IV. Instruksi Pelaksanaan Pengujian

1. **Persiapan**: 
   - Pastikan proyek dalam keadaan bersih
   - Untuk pengujian sistem, uji satu komponen pada satu waktu
   - Untuk pengujian algoritma, gunakan penguji otomatis dengan berbagai kombinasi

2. **Dokumentasi**:
   - Tandai setiap pengujian sebagai Lulus/Gagal dalam daftar periksa
   - Catat anomali atau perilaku tidak terduga
   - Dokumentasikan metrik kinerja jika berlaku

3. **Pengujian Regresi**:
   - Setelah memperbaiki bug, jalankan kembali pengujian terkait untuk memastikan lulus
   - Secara berkala jalankan rangkaian pengujian lengkap untuk menangkap regresi

4. **Pengujian Otomatis**:
   - Gunakan PathfindingTester untuk pengujian algoritma otomatis
   - Rekam dan analisis output CSV untuk perbandingan komprehensif 