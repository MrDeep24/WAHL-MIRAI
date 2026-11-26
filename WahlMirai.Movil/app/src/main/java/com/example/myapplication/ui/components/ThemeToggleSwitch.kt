package com.example.myapplication.ui.components

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.DarkMode
import androidx.compose.material.icons.filled.LightMode
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp

@Composable
fun ThemeToggleSwitch(
    isDarkTheme: Boolean,
    onToggleTheme: () -> Unit,
    modifier: Modifier = Modifier
) {
    val switchWidth = 92.dp
    val switchHeight = 44.dp
    val thumbSize = 36.dp

    val trackBgColor by animateColorAsState(
        targetValue = if (isDarkTheme) Color(0xFF172338) else Color(0xFFE2E8F0),
        animationSpec = tween(400),
        label = "trackBg"
    )

    val borderColor by animateColorAsState(
        targetValue = if (isDarkTheme) Color(0xFF334155) else Color(0xFFCBD5E1),
        animationSpec = tween(400),
        label = "trackBorder"
    )

    val thumbBgColor by animateColorAsState(
        targetValue = if (isDarkTheme) Color(0xFF1E293B) else Color(0xFFFFFFFF),
        animationSpec = tween(400),
        label = "thumbBg"
    )

    val thumbOffset by animateDpAsState(
        targetValue = if (isDarkTheme) 48.dp else 4.dp,
        animationSpec = tween(durationMillis = 350, easing = FastOutSlowInEasing),
        label = "thumbOffset"
    )

    val sunTint by animateColorAsState(
        targetValue = if (!isDarkTheme) Color(0xFFF59E0B) else Color(0xFF64748B),
        label = "sunTint"
    )

    val moonTint by animateColorAsState(
        targetValue = if (isDarkTheme) Color(0xFF60A5FA) else Color(0xFF94A3B8),
        label = "moonTint"
    )

    Box(
        modifier = modifier
            .width(switchWidth)
            .height(switchHeight)
            .clip(CircleShape)
            .background(trackBgColor)
            .border(1.dp, borderColor, CircleShape)
            .clickable(
                interactionSource = remember { MutableInteractionSource() },
                indication = null,
                onClick = onToggleTheme
            )
            .padding(4.dp),
        contentAlignment = Alignment.CenterStart
    ) {
        // Iconos de Fondo (Sol y Luna)
        Row(
            modifier = Modifier.fillMaxSize(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                modifier = Modifier.size(36.dp),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Default.LightMode,
                    contentDescription = "Modo Claro",
                    tint = sunTint,
                    modifier = Modifier.size(20.dp)
                )
            }

            Box(
                modifier = Modifier.size(36.dp),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Default.DarkMode,
                    contentDescription = "Modo Oscuro",
                    tint = moonTint,
                    modifier = Modifier.size(20.dp)
                )
            }
        }

        // Thumb Deslizante Animado
        Box(
            modifier = Modifier
                .offset(x = thumbOffset)
                .size(thumbSize)
                .shadow(elevation = 4.dp, shape = CircleShape)
                .background(thumbBgColor, CircleShape),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = if (isDarkTheme) Icons.Default.DarkMode else Icons.Default.LightMode,
                contentDescription = null,
                tint = if (isDarkTheme) Color(0xFF60A5FA) else Color(0xFFF59E0B),
                modifier = Modifier.size(20.dp)
            )
        }
    }
}
