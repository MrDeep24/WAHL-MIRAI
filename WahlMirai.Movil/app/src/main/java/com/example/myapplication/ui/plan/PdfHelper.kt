package com.example.myapplication.ui.plan

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.pdf.PdfDocument
import android.graphics.pdf.PdfRenderer
import android.os.ParcelFileDescriptor
import com.example.myapplication.data.models.Candidate
import com.example.myapplication.data.remote.ApiClient
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File
import java.io.FileOutputStream

data class LoadedPlanDocument(
    val bitmaps: List<Bitmap>,
    val isSizeValid: Boolean,
    val isUploadedDocument: Boolean
)

object PdfHelper {

    // Tamaño máximo de PDF permitido: 10 MB (10 * 1024 * 1024 bytes)
    const val MAX_PDF_SIZE_BYTES: Long = 10L * 1024L * 1024L

    fun isPdfSizeValid(file: File): Boolean {
        return file.exists() && file.length() <= MAX_PDF_SIZE_BYTES
    }

    suspend fun loadCandidatePlanDocument(context: Context, candidate: Candidate): LoadedPlanDocument {
        val rawUrl = candidate.pdfUrl?.trim().orEmpty()
        if (rawUrl.isNotEmpty()) {
            val bytes = ApiClient.downloadBytes(rawUrl)
            if (bytes != null && bytes.isNotEmpty()) {
                if (bytes.size.toLong() > MAX_PDF_SIZE_BYTES) {
                    return LoadedPlanDocument(
                        bitmaps = emptyList(),
                        isSizeValid = false,
                        isUploadedDocument = true
                    )
                }

                val isPdfBytes = bytes.size >= 4 &&
                    bytes[0] == 0x25.toByte() && // %
                    bytes[1] == 0x50.toByte() && // P
                    bytes[2] == 0x44.toByte() && // D
                    bytes[3] == 0x46.toByte()    // F

                if (isPdfBytes) {
                    val uploadedFile = withContext(Dispatchers.IO) {
                        File(context.cacheDir, "uploaded_plan_${candidate.id}.pdf").apply {
                            writeBytes(bytes)
                        }
                    }
                    val rendered = withContext(Dispatchers.IO) {
                        renderPdfToBitmaps(uploadedFile)
                    }
                    if (rendered.isNotEmpty()) {
                        return LoadedPlanDocument(
                            bitmaps = rendered,
                            isSizeValid = true,
                            isUploadedDocument = true
                        )
                    }
                } else {
                    val imageBitmap = BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
                    if (imageBitmap != null) {
                        return LoadedPlanDocument(
                            bitmaps = listOf(imageBitmap),
                            isSizeValid = true,
                            isUploadedDocument = true
                        )
                    }
                }
            }
        }

        return withContext(Dispatchers.IO) {
            val generatedFile = generateCandidatePdf(context, candidate)
            val valid = isPdfSizeValid(generatedFile)
            val bitmaps = if (valid) renderPdfToBitmaps(generatedFile) else emptyList()
            LoadedPlanDocument(
                bitmaps = bitmaps,
                isSizeValid = valid,
                isUploadedDocument = false
            )
        }
    }

    fun generateCandidatePdf(context: Context, candidate: Candidate): File {
        val pdfFile = File(context.cacheDir, "plan_gobierno_${candidate.id}.pdf")
        if (pdfFile.exists()) {
            pdfFile.delete()
        }

        val pdfDocument = PdfDocument()
        try {
            var pageNumber = 1
            var page = pdfDocument.startPage(PdfDocument.PageInfo.Builder(595, 842, pageNumber).create())
            var canvas: Canvas = page.canvas

            val paintHeaderBg = Paint().apply {
                color = Color.parseColor("#1E3A8A")
            }
            val paintTitle = Paint().apply {
                color = Color.WHITE
                textSize = 20f
                isFakeBoldText = true
            }
            val paintCandidateName = Paint().apply {
                color = Color.parseColor("#0F172A")
                textSize = 18f
                isFakeBoldText = true
            }
            val paintSubtitle = Paint().apply {
                color = Color.parseColor("#2563EB")
                textSize = 14f
                isFakeBoldText = true
            }
            val paintSectionHeader = Paint().apply {
                color = Color.parseColor("#1D4ED8")
                textSize = 15f
                isFakeBoldText = true
            }
            val paintText = Paint().apply {
                color = Color.parseColor("#334155")
                textSize = 13f
            }
            val paintSlogan = Paint().apply {
                color = Color.parseColor("#475569")
                textSize = 13f
            }
            val paintLine = Paint().apply {
                color = Color.parseColor("#E2E8F0")
                strokeWidth = 2f
            }

            var y = 130f
            val maxY = 790f

            fun ensureSpace(neededHeight: Float) {
                if (y + neededHeight > maxY) {
                    pdfDocument.finishPage(page)
                    pageNumber++
                    page = pdfDocument.startPage(PdfDocument.PageInfo.Builder(595, 842, pageNumber).create())
                    canvas = page.canvas
                    y = 56f
                }
            }

            // Encabezado institucional azul
            canvas.drawRect(0f, 0f, 595f, 90f, paintHeaderBg)
            canvas.drawText("PLAN DE GOBIERNO ESTUDIANTIL", 40f, 55f, paintTitle)

            // Nombre del candidato y detalles
            canvas.drawText(candidate.name, 40f, y, paintCandidateName)
            y += 25f

            canvas.drawText("Candidato N° ${candidate.number} • Grado ${candidate.grade}", 40f, y, paintSubtitle)
            y += 25f

            if (candidate.slogan.isNotBlank()) {
                canvas.drawText("\"${candidate.slogan}\"", 40f, y, paintSlogan)
                y += 35f
            } else {
                y += 10f
            }

            canvas.drawLine(40f, y, 555f, y, paintLine)
            y += 30f

            // Sección: Resumen de propuesta
            if (candidate.summary.isNotBlank()) {
                ensureSpace(50f)
                canvas.drawText("RESUMEN DE PROPUESTA", 40f, y, paintSectionHeader)
                y += 25f

                val summaryWords = candidate.summary.split(" ")
                var currentLine = ""
                for (word in summaryWords) {
                    if (paintText.measureText("$currentLine $word") < 515f) {
                        currentLine = if (currentLine.isEmpty()) word else "$currentLine $word"
                    } else {
                        ensureSpace(22f)
                        canvas.drawText(currentLine, 40f, y, paintText)
                        y += 20f
                        currentLine = word
                    }
                }
                if (currentLine.isNotEmpty()) {
                    ensureSpace(30f)
                    canvas.drawText(currentLine, 40f, y, paintText)
                    y += 30f
                }
            }

            // Sección: Propuestas de gobierno
            ensureSpace(50f)
            canvas.drawText("PROPUESTAS PRINCIPALES", 40f, y, paintSectionHeader)
            y += 25f

            for ((index, proposal) in candidate.proposals.withIndex()) {
                val fullText = "${index + 1}. ${proposal.text}"
                val words = fullText.split(" ")
                var line = ""
                for (word in words) {
                    if (paintText.measureText("$line $word") < 515f) {
                        line = if (line.isEmpty()) word else "$line $word"
                    } else {
                        ensureSpace(22f)
                        canvas.drawText(line, 40f, y, paintText)
                        y += 20f
                        line = "   $word"
                    }
                }
                if (line.isNotEmpty()) {
                    ensureSpace(26f)
                    canvas.drawText(line, 40f, y, paintText)
                    y += 25f
                }
            }

            pdfDocument.finishPage(page)
            FileOutputStream(pdfFile).use { out ->
                pdfDocument.writeTo(out)
            }
        } finally {
            pdfDocument.close()
        }

        return pdfFile
    }

    fun renderPdfToBitmaps(pdfFile: File): List<Bitmap> {
        val bitmaps = mutableListOf<Bitmap>()
        if (!isPdfSizeValid(pdfFile)) {
            return bitmaps
        }
        try {
            val fileDescriptor = ParcelFileDescriptor.open(pdfFile, ParcelFileDescriptor.MODE_READ_ONLY)
            val pdfRenderer = PdfRenderer(fileDescriptor)
            for (i in 0 until pdfRenderer.pageCount) {
                val page = pdfRenderer.openPage(i)
                val bitmap = Bitmap.createBitmap(
                    page.width * 2,
                    page.height * 2,
                    Bitmap.Config.ARGB_8888
                )
                val canvas = Canvas(bitmap)
                canvas.drawColor(Color.WHITE)
                page.render(bitmap, null, null, PdfRenderer.Page.RENDER_MODE_FOR_DISPLAY)
                bitmaps.add(bitmap)
                page.close()
            }
            pdfRenderer.close()
            fileDescriptor.close()
        } catch (e: Exception) {
            e.printStackTrace()
        }
        return bitmaps
    }
}
