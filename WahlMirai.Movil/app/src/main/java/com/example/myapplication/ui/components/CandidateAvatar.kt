package com.example.myapplication.ui.components

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.util.LruCache
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.HowToVote
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.data.remote.ApiClient
import com.example.myapplication.ui.theme.WahlTheme
import com.example.myapplication.ui.theme.WhiteVoteNeutralColor

private object ImageMemoryCache {
    private val cache = object : LruCache<String, Bitmap>(32) {}
    fun get(key: String): Bitmap? = cache.get(key)
    fun put(key: String, bitmap: Bitmap) {
        cache.put(key, bitmap)
    }
}

private fun initialsFor(name: String): String {
    val parts = name.trim().split(Regex("\\s+")).filter { it.isNotBlank() }
    if (parts.isEmpty()) return "?"
    if (parts.size == 1) return parts[0].take(2).uppercase()
    return "${parts[0].first()}${parts[1].first()}".uppercase()
}

@Composable
fun CandidateAvatar(
    name: String,
    number: String,
    photoUrl: String?,
    isWhiteVote: Boolean,
    size: Dp = 56.dp,
    shape: Shape = RoundedCornerShape(14.dp),
    showNumberBadge: Boolean = true,
    modifier: Modifier = Modifier
) {
    val colors = WahlTheme.colors
    val resolvedUrl = remember(photoUrl) { ApiClient.resolveUrl(photoUrl) }
    var bitmap by remember(resolvedUrl) {
        mutableStateOf(resolvedUrl?.let { ImageMemoryCache.get(it) })
    }

    LaunchedEffect(resolvedUrl, isWhiteVote) {
        if (!isWhiteVote && resolvedUrl != null && bitmap == null) {
            val bytes = ApiClient.downloadBytes(resolvedUrl)
            val decoded = if (bytes != null && bytes.isNotEmpty()) {
                BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
            } else {
                null
            }
            if (decoded != null) {
                ImageMemoryCache.put(resolvedUrl, decoded)
                bitmap = decoded
            }
        }
    }

    Box(modifier = modifier.size(size)) {
        val bgColor = when {
            isWhiteVote -> if (colors.isDark) Color(0xFF1E293B) else Color(0xFFF1F5F9)
            else -> colors.primarySoft
        }
        val borderColor = if (isWhiteVote) WhiteVoteNeutralColor.copy(alpha = 0.45f) else colors.primary.copy(alpha = 0.3f)

        Box(
            modifier = Modifier
                .fillMaxSize()
                .clip(shape)
                .background(bgColor)
                .border(1.dp, borderColor, shape),
            contentAlignment = Alignment.Center
        ) {
            when {
                isWhiteVote -> {
                    Icon(
                        imageVector = Icons.Default.HowToVote,
                        contentDescription = "Voto en Blanco",
                        tint = WhiteVoteNeutralColor,
                        modifier = Modifier.size(size * 0.46f)
                    )
                }
                bitmap != null -> {
                    Image(
                        bitmap = bitmap!!.asImageBitmap(),
                        contentDescription = name,
                        contentScale = ContentScale.Crop,
                        modifier = Modifier.fillMaxSize()
                    )
                }
                else -> {
                    Text(
                        text = initialsFor(name),
                        color = if (colors.isDark) Color(0xFF93C5FD) else colors.primaryContainer,
                        fontWeight = FontWeight.Bold,
                        fontSize = (size.value * 0.34f).sp
                    )
                }
            }
        }

        if (showNumberBadge && !isWhiteVote && number.isNotBlank()) {
            Surface(
                shape = CircleShape,
                color = colors.primaryContainer,
                shadowElevation = 2.dp,
                modifier = Modifier.align(Alignment.BottomEnd)
            ) {
                Text(
                    text = number,
                    color = Color.White,
                    fontFamily = FontFamily.Monospace,
                    fontWeight = FontWeight.Bold,
                    fontSize = if (size >= 80.dp) 13.sp else 10.sp,
                    modifier = Modifier.padding(horizontal = 6.dp, vertical = 2.dp)
                )
            }
        }
    }
}
