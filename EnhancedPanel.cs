using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyCustomControls
{
    [ToolboxItem(true)]
    public class EnhancedPanel : Panel
    {
        // --- Yeni Değişkenler ---
        private Image _currentIcon = null; // O an gösterilecek ikon
        private int _iconSize = 48; // İkonun kare boyutu (px)

        // --- Özellikler (Properties) ---
        private DashStyle _borderStyle = DashStyle.Solid;
        private Color _borderColor = Color.Black;
        private float _borderThickness = 1f;
        private int _borderRadius = 0;
        private string _placeholderText = "Dosyaları Buraya Sürükleyin";
        private Color _placeholderColor = Color.Gray;
        private Font _placeholderFont = new Font("Segoe UI", 12);

        // --- Yeni: Temizleme Butonu Değişkenleri ---
        private Rectangle _clearButtonRect;
        private bool _showClearButton = false;
        private Color _clearButtonHoverColor = Color.Red;
        private bool _isMouseOverClear = false;

        // --- Geçici Görsel Durum Değişkenleri ---
        private Color _tempBorderColor;
        private float _tempBorderThickness;
        private bool _isDragging = false;

        [Category("Appearance Custom")]
        public Color DragOverBorderColor { get; set; } = Color.RoyalBlue; // Sürükleme anındaki renk

        [Category("Appearance Custom")]
        public float DragOverBorderThickness { get; set; } = 3f; // Sürükleme anındaki kalınlık

        [Category("Appearance Custom")]
        public DashStyle BorderDashStyle { get => _borderStyle; set { _borderStyle = value; Invalidate(); } }

        [Category("Appearance Custom")]
        public Color BorderColor { get => _borderColor; set { _borderColor = value; Invalidate(); } }

        [Category("Appearance Custom")]
        public float BorderThickness { get => _borderThickness; set { _borderThickness = value; Invalidate(); } }

        [Category("Appearance Custom")]
        public int BorderRadius { get => _borderRadius; set { _borderRadius = value; Invalidate(); } }

        [Category("Appearance Custom")]
        public string PlaceholderText { get => _placeholderText; set { _placeholderText = value; Invalidate(); } }

        [Category("Appearance Custom")]
        public Color PlaceholderColor { get => _placeholderColor; set { _placeholderColor = value; Invalidate(); } }

        [Category("Appearance Custom")]
        public Font PlaceholderFont { get => _placeholderFont; set { _placeholderFont = value; Invalidate(); } }

        // Constructor içine DragDrop ayarlarını ekle
        public EnhancedPanel()
        {
            // Titremeyi önlemek için DoubleBuffering aktif
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.AllowDrop = true; // Drag-Drop desteği
            _originalPlaceholder = _placeholderText; // Varsayılanı yedekle
        }
        // --- Temizleme Mantığı ---
        public void ClearContent()
        {
            _currentIcon = null;
            _placeholderText = _originalPlaceholder;
            _showClearButton = false;
            this.Invalidate();
        }

        // Fare hareketlerini izleyerek butona tıklandığını anlayalım
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_showClearButton)
            {
                bool isOver = _clearButtonRect.Contains(e.Location);
                if (isOver != _isMouseOverClear)
                {
                    _isMouseOverClear = isOver;
                    this.Cursor = isOver ? Cursors.Hand : Cursors.Default;
                    this.Invalidate(_clearButtonRect); // Sadece butonu yeniden çiz
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_showClearButton && _clearButtonRect.Contains(e.Location))
            {
                ClearContent();
            }
        }
        // --- Drag/Drop Mantığı ---
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            Invalidate(); // Nesne eklendiğinde placeholder'ı gizlemek için yeniden çiz
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            Invalidate(); // Nesne silindiğinde placeholder'ı göstermek için yeniden çiz
        }

        // --- Çizim İşlemleri ---
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            // 1. Placeholder ve İkon Çizimi
            if (this.Controls.Count == 0 && !string.IsNullOrEmpty(_placeholderText))
            {
                using (Brush brush = new SolidBrush(_placeholderColor))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                    int totalContentHeight = _iconSize + (int)_placeholderFont.Height + 10; // İkon + Boşluk + Metin
                    int startY = (this.Height - totalContentHeight) / 2;

                    // İkon Varsa Çiz
                    if (_currentIcon != null)
                    {
                        Rectangle iconRect = new Rectangle((this.Width - _iconSize) / 2, startY, _iconSize, _iconSize);
                        g.DrawImage(_currentIcon, iconRect);
                        startY += _iconSize + 10; // Metni ikonun altına kaydır
                    }

                    // Metni Çiz
                    RectangleF textRect = new RectangleF(10, startY, this.Width - 20, _placeholderFont.Height * 2);
                    g.DrawString(_placeholderText, _placeholderFont, brush, textRect, sf);
                }
            }

            // 2. Kenar Çizgisi (Border) ve Yuvarlatma Çizimi
            using (GraphicsPath path = GetRoundRectangle(rect, _borderRadius))
            {
                using (Pen pen = new Pen(_borderColor, _borderThickness))
                {
                    pen.DashStyle = _borderStyle;
                    // Köşe birleşimlerini yumuşat
                    pen.LineJoin = LineJoin.Round;
                    g.DrawPath(pen, path);
                }
            }

            // 3. Temizleme (Çöp Kutusu) Butonu Çizimi
            if (_showClearButton)
            {
                DrawClearButton(g);
            }
        }
        private void DrawClearButton(Graphics g)
        {
            int size = 24;
            int margin = 10;
            // Sağ üst köşeye yerleştir
            _clearButtonRect = new Rectangle(this.Width - size - margin, margin, size, size);

            using (Pen pen = new Pen(_isMouseOverClear ? Color.Red : Color.Gray, 2))
            {
                // Basit bir "X" veya Çöp Kutusu simgesi çizelim
                g.DrawEllipse(pen, _clearButtonRect);
                g.DrawLine(pen, _clearButtonRect.X + 6, _clearButtonRect.Y + 6, _clearButtonRect.Right - 6, _clearButtonRect.Bottom - 6);
                g.DrawLine(pen, _clearButtonRect.Right - 6, _clearButtonRect.Y + 6, _clearButtonRect.X + 6, _clearButtonRect.Bottom - 6);
                //g.DrawImage(global::EnhancedPanel.Properties.Resources.logo, _clearButtonRect);
            }
        }
        // Yuvarlatılmış dikdörtgen path'i oluşturan yardımcı metod
        private GraphicsPath GetRoundRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
        // --- Sürükle-Bırak Mantığı İçin Gerekli Değişkenler ---
        private string _originalPlaceholder; // Orijinal metni saklamak için




        // 1. Dosya Panel Üzerine Geldiğinde
        // --- Olay Yönetimi ---

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);

            if (drgevent.Data.GetDataPresent(DataFormats.FileDrop))
            {
                drgevent.Effect = DragDropEffects.Copy;

                // Mevcut ayarları yedekle
                _tempBorderColor = this.BorderColor;
                _tempBorderThickness = this.BorderThickness;
                _isDragging = true;

                // Görseli vurgula
                this.BorderColor = DragOverBorderColor;
                this.BorderThickness = DragOverBorderThickness;
                _placeholderText = "Dosyayı Bırakın...";

                this.Invalidate(); // Yeniden çiz
            }
        }

        // 2. Dosya Panelden Ayrıldığında (Bırakmadan Vazgeçilirse)
        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            ResetVisualState();
        }

        // 3. Dosya Bırakıldığında (Drop İşlemi)
        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
            string[] files = (string[])drgevent.Data.GetData(DataFormats.FileDrop);
            ResetVisualState();

            if (files.Length > 0)
            {
                string filePath = files[0];
                _placeholderText = System.IO.Path.GetFileName(filePath);

                // Uzantıya göre ikon ata
                _currentIcon = GetSystemIcon(filePath);
                _showClearButton = true; // Butonu göster
                this.Invalidate();
            }
        }
        // Windows sistem ikonunu çeken yardımcı metod
        private Image GetSystemIcon(string filePath)
        {
            try
            {
                using (Icon sysIcon = Icon.ExtractAssociatedIcon(filePath))
                {
                    return sysIcon.ToBitmap();
                }
            }
            catch { return null; }
        }
        private void ResetVisualState()
        {
            if (_isDragging)
            {
                this.BorderColor = _tempBorderColor;
                this.BorderThickness = _tempBorderThickness;
                _placeholderText = _originalPlaceholder;
                _isDragging = false;
                this.Invalidate();
            }
        }
    }
}