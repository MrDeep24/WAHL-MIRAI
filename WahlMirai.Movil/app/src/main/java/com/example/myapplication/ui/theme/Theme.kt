package com.example.myapplication.ui.theme

import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.dynamicDarkColorScheme
import androidx.compose.material3.dynamicLightColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext

@Immutable
data class WahlPalette(
    val isDark: Boolean,
    val background: Color,
    val card: Color,
    val surfaceLow: Color,
    val surfaceHigh: Color,
    val text: Color,
    val subtext: Color,
    val border: Color,
    val progressTrack: Color,
    val primary: Color,
    val primaryContainer: Color,
    val primarySoft: Color,
    val accentText: Color,
    val confirm: Color,
    val successText: Color,
    val successBg: Color,
    val error: Color,
    val errorBg: Color
)

private val LightWahlPalette = WahlPalette(
    isDark = false,
    background = WahlLightBg,
    card = WahlLightCard,
    surfaceLow = WahlLightSurfaceLow,
    surfaceHigh = WahlLightSurfaceHigh,
    text = WahlLightText,
    subtext = WahlLightSubtext,
    border = WahlLightBorder,
    progressTrack = WahlLightTrack,
    primary = WahlBlueAccent,
    primaryContainer = WahlNavy,
    primarySoft = WahlBlueSoft,
    accentText = WahlBlue,
    confirm = WahlConfirmBright,
    successText = Color(0xFF065F46),
    successBg = WahlSuccessSoftLight,
    error = WahlErrorLight,
    errorBg = WahlErrorSoftLight
)

private val DarkWahlPalette = WahlPalette(
    isDark = true,
    background = WahlDarkBg,
    card = WahlDarkCard,
    surfaceLow = WahlDarkSurfaceLow,
    surfaceHigh = WahlDarkSurfaceHigh,
    text = WahlDarkText,
    subtext = WahlDarkSubtext,
    border = WahlDarkBorder,
    progressTrack = WahlDarkTrack,
    primary = WahlBlueBright,
    primaryContainer = Color(0xFF1E3A8A),
    primarySoft = Color(0xFF172B4D),
    accentText = WahlBlueSky,
    confirm = WahlConfirmBright,
    successText = Color(0xFF34D399),
    successBg = WahlSuccessSoftDark,
    error = WahlErrorDark,
    errorBg = WahlErrorSoftDark
)

val LocalWahlPalette = staticCompositionLocalOf { DarkWahlPalette }

object WahlTheme {
    val colors: WahlPalette
        @Composable
        @ReadOnlyComposable
        get() = LocalWahlPalette.current
}

private val DarkColorScheme = darkColorScheme(
    primary = WahlBlueBright,
    onPrimary = Color.White,
    primaryContainer = Color(0xFF1E3A8A),
    onPrimaryContainer = WahlBlueSoft,
    secondary = WahlSuccessGreen,
    onSecondary = Color.White,
    background = WahlDarkBg,
    onBackground = WahlDarkText,
    surface = WahlDarkCard,
    onSurface = WahlDarkText,
    surfaceVariant = WahlDarkSurfaceLow,
    onSurfaceVariant = WahlDarkSubtext,
    outline = WahlDarkBorder,
    error = WahlErrorDark,
    onError = Color.White
)

private val LightColorScheme = lightColorScheme(
    primary = WahlNavy,
    onPrimary = Color.White,
    primaryContainer = WahlBlue,
    onPrimaryContainer = WahlBlueSoft,
    secondary = WahlConfirmGreen,
    onSecondary = Color.White,
    background = WahlLightBg,
    onBackground = WahlLightText,
    surface = WahlLightCard,
    onSurface = WahlLightText,
    surfaceVariant = WahlLightSurfaceLow,
    onSurfaceVariant = WahlLightSubtext,
    outline = WahlLightBorder,
    error = WahlErrorLight,
    onError = Color.White
)

@Composable
fun MyApplicationTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    dynamicColor: Boolean = false,
    content: @Composable () -> Unit
) {
    val colorScheme = when {
        dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> {
            val context = LocalContext.current
            if (darkTheme) dynamicDarkColorScheme(context) else dynamicLightColorScheme(context)
        }
        darkTheme -> DarkColorScheme
        else -> LightColorScheme
    }

    val wahlPalette = if (darkTheme) DarkWahlPalette else LightWahlPalette

    CompositionLocalProvider(LocalWahlPalette provides wahlPalette) {
        MaterialTheme(
            colorScheme = colorScheme,
            typography = Typography,
            content = content
        )
    }
}
