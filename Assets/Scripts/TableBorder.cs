using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class TableBorder : MonoBehaviour
{
    [Header("Ukuran Struktur (Header & Jarak)")]
    public float headerHeight = 135f;         // Jarak dari atas Log Table sampai awal Panel baris
    public float fallbackRowHeight = 36f;     // Tinggi cadangan jika Panel kosong
    public float rowSpacing = 4f;             // Cadangan spacing jika Panel tidak pakai VerticalLayoutGroup
    public float panelTopBottomPad = 12f;     // Cadangan padding atas+bawah jika Panel tidak pakai VerticalLayoutGroup
    public float bottomLineGap = 8f;          // Jarak bersih antara baris transaksi terakhir dengan garis bawah tabel
    public float tableBottomMargin = 20f;     // Jarak kosong di luar bawah kotak tabel sebelum tabel hari berikutnya

    [Header("Posisi Garis Horizontal (Diukur dari Atas Log Table)")]
    public float boxTopY = 28f;               // Garis atas kotak (di bawah "Day N", di atas BUY / SELL / G/L / G/L%)
    public float midHeaderLineY = 54f;        // Jarak dari atas kotak ke garis pemisah "BUY/SELL" & "Saham Vol Price..."
    public float bottomHeaderLineY = 98f;     // Jarak dari atas kotak ke garis bawah judul kolom (di atas baris pertama)

    [Header("Pengaturan Garis")]
    public Color lineColor = Color.white;
    public float lineThickness = 2f;
    public float leftRightMargin = 0f;

    [Header("Posisi X Garis Vertikal Pemisah Kolom (Rasio 0.0 - 1.0)")]
    [Range(0f, 1f)] public float buySellDividerX = 0.39f;    // Pemisah kolom BUY dan SELL
    [Range(0f, 1f)] public float sellGLDividerX = 0.81f;     // Pemisah kolom SELL dan G/L
    [Range(0f, 1f)] public float glPercentDividerX = 0.905f; // Pemisah kolom G/L dan G/L%

    private RectTransform borderContainer;

    public void BuildBorders()
    {
        RectTransform tableRect = GetComponent<RectTransform>();
        tableRect.pivot = new Vector2(0.5f, 1f);

        Transform panelTrans = transform.Find("Panel");
        if (panelTrans == null) panelTrans = transform.Find("RowContainer");

        int rowCount = panelTrans != null ? panelTrans.childCount : 0;
        float targetWidth = tableRect.rect.width > 1f ? tableRect.rect.width : tableRect.sizeDelta.x;

        // 1. Baca Spacing & Padding asli langsung dari VerticalLayoutGroup milik Panel
        float actualSpacing = rowSpacing;
        float actualPadTop = panelTopBottomPad * 0.5f;
        float actualPadBottom = panelTopBottomPad * 0.5f;

        if (panelTrans != null)
        {
            VerticalLayoutGroup vlg = panelTrans.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                actualSpacing = vlg.spacing;
                actualPadTop = vlg.padding.top;
                actualPadBottom = vlg.padding.bottom;
            }
        }

        // 2. Pastikan lebar setiap baris LogPanel terjaga (tidak 0 saat Control Child Size Width dimatikan)
        //    dan jumlahkan tinggi aktual seluruh baris transaksi
        float sumRowsHeight = 0f;
        if (panelTrans != null && rowCount > 0)
        {
            for (int i = 0; i < rowCount; i++)
            {
                if (panelTrans.GetChild(i) is RectTransform rowRect)
                {
                    if (rowRect.rect.width <= 1f && targetWidth > 1f)
                    {
                        rowRect.anchorMin = new Vector2(0.5f, 1f);
                        rowRect.anchorMax = new Vector2(0.5f, 1f);
                        rowRect.pivot = new Vector2(0.5f, 1f);
                        rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                    }

                    float h = rowRect.rect.height > 1f ? rowRect.rect.height : rowRect.sizeDelta.y;
                    if (h <= 1f) h = fallbackRowHeight;
                    sumRowsHeight += h;
                }
            }
        }
        else
        {
            sumRowsHeight = fallbackRowHeight;
        }

        // 3. Hitung tinggi total area Panel secara presisi berdasarkan isi baris aktual
        float totalSpacing = rowCount > 1 ? (rowCount - 1) * actualSpacing : 0f;
        float rowsAreaHeight = actualPadTop + sumRowsHeight + totalSpacing + actualPadBottom;

        if (panelTrans is RectTransform panelRect)
        {
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -headerHeight);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowsAreaHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        // 4. Hitung posisi pasti bagian bawah konten (baris terakhir + bottomLineGap)
        //    agar garis bawah kotak menempel rapat dan tidak menyisakan ruang kosong berlebih
        float contentBottomFromTop = headerHeight + actualPadTop + sumRowsHeight + totalSpacing + bottomLineGap;
        float totalTableHeight = contentBottomFromTop + tableBottomMargin;

        tableRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalTableHeight);

        LayoutElement le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.minHeight = totalTableHeight;
        le.preferredHeight = totalTableHeight;

        // 5. Hapus overlay garis lama jika di-refresh ulang
        Transform old = transform.Find("AutoBorderOverlay");
        if (old != null)
        {
            DestroyImmediate(old.gameObject);
        }

        // 6. Buat wadah kotak tabel yang membungkus tepat dari boxTopY hingga garis bawah baris terakhir
        GameObject overlayObj = new GameObject("AutoBorderOverlay", typeof(RectTransform));
        overlayObj.transform.SetParent(this.transform, false);

        borderContainer = overlayObj.GetComponent<RectTransform>();
        borderContainer.anchorMin = Vector2.zero;
        borderContainer.anchorMax = Vector2.one;
        borderContainer.offsetMin = new Vector2(leftRightMargin, tableBottomMargin);
        borderContainer.offsetMax = new Vector2(-leftRightMargin, -boxTopY);

        // 7. Gambar Bingkai Luar (Kiri, Kanan, Atas, Bawah)
        CreateLine("Border_Left",   new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, lineThickness, 0f);
        CreateLine("Border_Right",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, lineThickness, 0f);
        CreateLine("Border_Top",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, 0f, lineThickness);
        CreateLine("Border_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, 0f, lineThickness);

        // 8. Gambar Garis Horizontal Header
        CreateLine("Header_MidLine",    new Vector2(0f, 1f), new Vector2(sellGLDividerX, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -midHeaderLineY), 0f, lineThickness);
        CreateLine("Header_BottomLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -bottomHeaderLineY), 0f, lineThickness);

        // 9. Gambar 3 Garis Vertikal Pemisah Kolom
        CreateLine("Col_BuySell",   new Vector2(buySellDividerX, 0f),   new Vector2(buySellDividerX, 1f),   new Vector2(0.5f, 0.5f), Vector2.zero, lineThickness, 0f);
        CreateLine("Col_SellGL",    new Vector2(sellGLDividerX, 0f),    new Vector2(sellGLDividerX, 1f),    new Vector2(0.5f, 0.5f), Vector2.zero, lineThickness, 0f);
        CreateLine("Col_GLPercent", new Vector2(glPercentDividerX, 0f), new Vector2(glPercentDividerX, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, lineThickness, 0f);
    }

    private void CreateLine(string lineName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, float width, float height)
    {
        GameObject lineObj = new GameObject(lineName, typeof(RectTransform), typeof(Image));
        lineObj.transform.SetParent(borderContainer, false);

        Image img = lineObj.GetComponent<Image>();
        img.color = lineColor;
        img.raycastTarget = false;

        RectTransform rt = lineObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(width, height);
    }
}