package com.example.myapplication.ui.components

import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.Path
import android.graphics.Shader
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.ShaderBrush
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.dp
import com.example.myapplication.ui.theme.WahlTheme
import kotlin.math.roundToInt

/**
 * Fondo de mosaico electoral idéntico a `voting-pattern.svg` de WahlMirai.Web,
 * renderizado como patrón continuo (TileMode.REPEAT) sobre un ShaderBrush.
 */
@Composable
fun VotingPatternBackground(modifier: Modifier = Modifier) {
    val colors = WahlTheme.colors
    val density = LocalDensity.current
    val tileSizePx = with(density) { 340.dp.toPx().roundToInt().coerceAtLeast(200) }
    val strokeColor = if (colors.isDark) {
        androidx.compose.ui.graphics.Color.White.copy(alpha = 0.14f)
    } else {
        androidx.compose.ui.graphics.Color.Black.copy(alpha = 0.16f)
    }
    val dotColor = if (colors.isDark) {
        androidx.compose.ui.graphics.Color.White.copy(alpha = 0.055f)
    } else {
        androidx.compose.ui.graphics.Color.Black.copy(alpha = 0.06f)
    }

    val brush = remember(tileSizePx, strokeColor, dotColor) {
        val bitmap = createVotingPatternTile(
            sizePx = tileSizePx,
            strokeArgb = strokeColor.toArgb(),
            dotArgb = dotColor.toArgb()
        )
        val shader = android.graphics.BitmapShader(
            bitmap,
            Shader.TileMode.REPEAT,
            Shader.TileMode.REPEAT
        )
        ShaderBrush(shader)
    }

    Canvas(modifier = modifier.fillMaxSize()) {
        drawRect(brush = brush)
    }
}

private fun createVotingPatternTile(
    sizePx: Int,
    strokeArgb: Int,
    dotArgb: Int
): Bitmap {
    val bitmap = Bitmap.createBitmap(sizePx, sizePx, Bitmap.Config.ARGB_8888)
    val canvas = Canvas(bitmap)
    val scale = sizePx / 400f
    canvas.scale(scale, scale)

    val stroke = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = strokeArgb
        style = Paint.Style.STROKE
        strokeWidth = 1.5f
        strokeCap = Paint.Cap.ROUND
        strokeJoin = Paint.Join.ROUND
    }
    val thinStroke = Paint(stroke).apply { strokeWidth = 1.2f }
    val thickStroke = Paint(stroke).apply { strokeWidth = 2.2f }
    val dotFill = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = dotArgb
        style = Paint.Style.FILL
    }

    fun drawAt(x: Float, y: Float, degrees: Float, drawFn: () -> Unit) {
        canvas.save()
        canvas.translate(x, y)
        canvas.rotate(degrees)
        drawFn()
        canvas.restore()
    }

    fun drawBallotBox() {
        canvas.drawRoundRect(-16f, -2f, 16f, 16f, 2f, 2f, stroke)
        canvas.drawLine(-18f, -2f, 18f, -2f, stroke)
        canvas.drawLine(-7f, -2f, 7f, -2f, thickStroke)
        canvas.drawCircle(0f, 6f, 2f, stroke)
        canvas.drawLine(0f, 8f, 0f, 11f, stroke)

        canvas.save()
        canvas.rotate(-6f)
        canvas.drawRoundRect(-5f, -12f, 5f, -1f, 1f, 1f, stroke)
        canvas.drawLine(-2f, -8f, 2f, -8f, stroke)
        canvas.restore()

        val hand = Path().apply {
            moveTo(12f, -16f)
            cubicTo(10f, -13f, 6f, -11f, 1f, -11f)
            moveTo(14f, -13f)
            cubicTo(11f, -11f, 7f, -9f, 2f, -9f)
            moveTo(16f, -9f)
            cubicTo(13f, -7f, 8f, -6f, 3f, -6f)
        }
        canvas.drawPath(hand, stroke)
    }

    fun drawCheckCircle() {
        canvas.drawCircle(0f, 0f, 14f, stroke)
        val check = Path().apply {
            moveTo(-7f, 0f)
            lineTo(-2f, 5f)
            lineTo(7f, -5f)
        }
        canvas.drawPath(check, stroke)
    }

    fun drawBallotPaper() {
        val doc = Path().apply {
            moveTo(-11f, -15f)
            lineTo(5f, -15f)
            lineTo(11f, -9f)
            lineTo(11f, 15f)
            lineTo(-11f, 15f)
            close()
            moveTo(5f, -15f)
            lineTo(5f, -9f)
            lineTo(11f, -9f)
        }
        canvas.drawPath(doc, stroke)
        canvas.drawRoundRect(-8f, -7f, -2f, -1f, 1f, 1f, stroke)
        val check = Path().apply {
            moveTo(-7f, -4f)
            lineTo(-5f, -2f)
            lineTo(-1f, -7f)
        }
        canvas.drawPath(check, stroke)
        canvas.drawLine(1f, -4f, 8f, -4f, stroke)
        canvas.drawRoundRect(-8f, 3f, -2f, 9f, 1f, 1f, stroke)
        canvas.drawLine(1f, 6f, 8f, 6f, stroke)
    }

    fun drawRaisedHand() {
        val hand = Path().apply {
            moveTo(-6f, 13f)
            lineTo(-6f, 3f)
            cubicTo(-6f, 1f, -4f, 0f, -2f, 0f)
            cubicTo(-1f, 0f, 0f, 1f, 0f, 2f)
            cubicTo(1f, 1f, 2f, 0f, 4f, 0f)
            cubicTo(5f, 0f, 6f, 1f, 6f, 2f)
            cubicTo(7f, 1f, 8f, 1f, 9f, 2f)
            cubicTo(9.5f, 2.5f, 9.5f, 3.5f, 9.5f, 4.5f)
            lineTo(9.5f, 7f)
            cubicTo(9.5f, 10f, 7f, 13f, 4f, 13f)
            close()
            moveTo(-6f, 6f)
            cubicTo(-8f, 6f, -10f, 4f, -9f, 2f)
            cubicTo(-8f, 0f, -6f, 1f, -4f, 3f)
        }
        canvas.drawPath(hand, stroke)
        canvas.drawLine(-6f, 13f, -6f, 17f, stroke)
        canvas.drawLine(4f, 13f, 4f, 17f, stroke)
        canvas.drawLine(-1f, 2f, -1f, 7f, thinStroke)
        canvas.drawLine(4f, 2f, 4f, 7f, thinStroke)
    }

    fun drawMegaphone() {
        val cone = Path().apply {
            moveTo(-8f, -3f)
            lineTo(6f, -10f)
            lineTo(6f, 10f)
            lineTo(-8f, 3f)
            close()
        }
        canvas.drawPath(cone, stroke)
        canvas.drawRoundRect(-12f, -3f, -8f, 3f, 1f, 1f, stroke)
        val handle = Path().apply {
            moveTo(-5f, 3f)
            lineTo(-4f, 11f)
            cubicTo(-4f, 12f, -3f, 13f, -2f, 13f)
            lineTo(0f, 13f)
            cubicTo(1f, 13f, 1.5f, 12f, 1f, 11f)
            lineTo(-0.5f, 7f)
        }
        canvas.drawPath(handle, stroke)
        val waves = Path().apply {
            moveTo(10f, -5f)
            cubicTo(12f, -2f, 12f, 2f, 10f, 5f)
            moveTo(14f, -8f)
            cubicTo(17f, -3f, 17f, 3f, 14f, 8f)
        }
        canvas.drawPath(waves, stroke)
    }

    fun drawCalendar() {
        canvas.drawRoundRect(-13f, -11f, 13f, 13f, 3f, 3f, stroke)
        canvas.drawLine(-13f, -4f, 13f, -4f, stroke)
        canvas.drawLine(-7f, -14f, -7f, -9f, stroke)
        canvas.drawLine(7f, -14f, 7f, -9f, stroke)
        canvas.drawCircle(0f, 4f, 4.5f, stroke)
        val check = Path().apply {
            moveTo(-2f, 4f)
            lineTo(-0.5f, 5.5f)
            lineTo(2.5f, 2.5f)
        }
        canvas.drawPath(check, stroke)
        val calDot = Paint(stroke).apply { style = Paint.Style.FILL }
        canvas.drawCircle(-7f, 2f, 1f, calDot)
        canvas.drawCircle(7f, 2f, 1f, calDot)
        canvas.drawCircle(-7f, 8f, 1f, calDot)
        canvas.drawCircle(7f, 8f, 1f, calDot)
    }

    fun drawIdCard() {
        canvas.drawRoundRect(-15f, -11f, 15f, 11f, 3f, 3f, stroke)
        canvas.drawCircle(-7f, -3f, 3f, stroke)
        val shoulders = Path().apply {
            moveTo(-11f, 5f)
            cubicTo(-11f, 3f, -9f, 1f, -7f, 1f)
            cubicTo(-5f, 1f, -3f, 3f, -3f, 5f)
        }
        canvas.drawPath(shoulders, stroke)
        canvas.drawLine(2f, -5f, 11f, -5f, stroke)
        canvas.drawLine(2f, -1f, 9f, -1f, stroke)
        canvas.drawLine(2f, 3f, 7f, 3f, stroke)
        canvas.drawRoundRect(-3f, -14f, 3f, -11f, 1f, 1f, thinStroke)
    }

    fun drawSpeechBubble() {
        val bubble = Path().apply {
            moveTo(-13f, -9f)
            cubicTo(-13f, -13f, -9f, -14f, 0f, -14f)
            cubicTo(9f, -14f, 13f, -13f, 13f, -9f)
            lineTo(13f, 3f)
            cubicTo(13f, 7f, 9f, 8f, 0f, 8f)
            lineTo(-4f, 8f)
            lineTo(-8f, 12f)
            lineTo(-7f, 8f)
            lineTo(-9f, 8f)
            cubicTo(-13f, 8f, -13f, 7f, -13f, 3f)
            close()
        }
        canvas.drawPath(bubble, stroke)
        val check = Path().apply {
            moveTo(-4f, -3f)
            lineTo(-1f, 0f)
            lineTo(5f, -6f)
        }
        canvas.drawPath(check, stroke)
    }

    fun drawFlag() {
        canvas.drawLine(-8f, -15f, -8f, 15f, stroke)
        canvas.drawCircle(-8f, -15f, 1.5f, stroke)
        val flag = Path().apply {
            moveTo(-8f, -13f)
            cubicTo(-2f, -15f, 2f, -11f, 8f, -13f)
            lineTo(8f, -2f)
            cubicTo(2f, 0f, -2f, -4f, -8f, -2f)
            close()
        }
        canvas.drawPath(flag, stroke)
    }

    fun drawSearchDoc() {
        val doc = Path().apply {
            moveTo(-11f, -13f)
            lineTo(2f, -13f)
            lineTo(6f, -9f)
            lineTo(6f, 7f)
            lineTo(-11f, 7f)
            close()
        }
        canvas.drawPath(doc, stroke)
        canvas.drawLine(-7f, -8f, -2f, -8f, stroke)
        canvas.drawLine(-7f, -4f, 0f, -4f, stroke)
        canvas.drawCircle(3f, 3f, 7f, stroke)
        canvas.drawLine(8f, 8f, 13f, 13f, thickStroke)
    }

    // Row 0 (y = 40)
    drawAt(40f, 40f, -8f) { drawBallotBox() }
    drawAt(140f, 40f, 10f) { drawCheckCircle() }
    drawAt(240f, 40f, -12f) { drawBallotPaper() }
    drawAt(340f, 40f, 8f) { drawMegaphone() }

    // Row 1 (y = 120, offset x + wrap)
    drawAt(90f, 120f, -6f) { drawRaisedHand() }
    drawAt(190f, 120f, 8f) { drawCalendar() }
    drawAt(290f, 120f, -10f) { drawIdCard() }
    drawAt(390f, 120f, 12f) { drawSpeechBubble() }
    drawAt(-10f, 120f, 12f) { drawSpeechBubble() }

    // Row 2 (y = 200)
    drawAt(40f, 200f, -10f) { drawFlag() }
    drawAt(140f, 200f, 14f) { drawSearchDoc() }
    drawAt(240f, 200f, 6f) { drawBallotBox() }
    drawAt(340f, 200f, -8f) { drawCheckCircle() }

    // Row 3 (y = 280, offset x + wrap)
    drawAt(90f, 280f, 10f) { drawBallotPaper() }
    drawAt(190f, 280f, -8f) { drawRaisedHand() }
    drawAt(290f, 280f, -14f) { drawMegaphone() }
    drawAt(390f, 280f, 6f) { drawCalendar() }
    drawAt(-10f, 280f, 6f) { drawCalendar() }

    // Row 4 (y = 360)
    drawAt(40f, 360f, -12f) { drawSpeechBubble() }
    drawAt(140f, 360f, 10f) { drawFlag() }
    drawAt(240f, 360f, -6f) { drawSearchDoc() }
    drawAt(340f, 360f, 8f) { drawIdCard() }

    // Micro-dots
    val dots = listOf(
        90f to 40f, 190f to 40f, 290f to 40f,
        40f to 120f, 140f to 120f, 240f to 120f, 340f to 120f,
        90f to 200f, 190f to 200f, 290f to 200f,
        40f to 280f, 140f to 280f, 240f to 280f, 340f to 280f,
        90f to 360f, 190f to 360f, 290f to 360f
    )
    for ((dx, dy) in dots) {
        canvas.drawCircle(dx, dy, 1.2f, dotFill)
    }

    return bitmap
}
