package com.example.myapplication.ui.perfil

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.myapplication.ui.components.ThemeToggleSwitch
import com.example.myapplication.ui.theme.WahlTheme

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PerfilScreen(
    viewModel: PerfilViewModel = viewModel(),
    isDarkTheme: Boolean = true,
    onToggleTheme: () -> Unit = {},
    onBack: () -> Unit,
    onCerrarSesion: () -> Unit
) {
    val uiState by viewModel.uiState.collectAsState()
    val user = uiState.user
    val colors = WahlTheme.colors
    val context = LocalContext.current

    Scaffold(
        topBar = {
            CenterAlignedTopAppBar(
                title = {
                    Text(
                        text = "Perfil de Usuario",
                        fontWeight = FontWeight.Bold,
                        color = colors.text
                    )
                },
                navigationIcon = {
                    IconButton(
                        onClick = onBack,
                        modifier = Modifier
                            .padding(start = 8.dp)
                            .background(colors.surfaceLow, CircleShape)
                    ) {
                        Icon(
                            Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = "Volver",
                            tint = colors.accentText
                        )
                    }
                },
                actions = {
                    Spacer(modifier = Modifier.width(56.dp))
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = colors.background.copy(alpha = 0.94f),
                    titleContentColor = colors.text
                )
            )
        },
        containerColor = Color.Transparent
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(20.dp))

            // Avatar con iniciales institucionales
            Surface(
                shape = RoundedCornerShape(24.dp),
                color = colors.primarySoft,
                border = BorderStroke(2.dp, colors.primary.copy(alpha = 0.25f)),
                shadowElevation = if (colors.isDark) 0.dp else 4.dp,
                modifier = Modifier.size(96.dp)
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Text(
                        text = user.nombre.firstOrNull()?.toString()?.uppercase() ?: "W",
                        color = if (colors.isDark) Color(0xFF93C5FD) else colors.primaryContainer,
                        fontWeight = FontWeight.Bold,
                        fontSize = 44.sp
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = user.nombre,
                color = colors.text,
                fontSize = 22.sp,
                fontWeight = FontWeight.Bold
            )

            Spacer(modifier = Modifier.height(10.dp))

            // Etiquetas de Rol y Estado
            Row(
                horizontalArrangement = Arrangement.spacedBy(10.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(
                    modifier = Modifier
                        .background(colors.surfaceLow, RoundedCornerShape(20.dp))
                        .border(1.dp, colors.border, RoundedCornerShape(20.dp))
                        .padding(horizontal = 14.dp, vertical = 6.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    Icon(
                        Icons.Default.Person,
                        contentDescription = null,
                        tint = colors.subtext,
                        modifier = Modifier.size(14.dp)
                    )
                    Text(user.rol, color = colors.text, fontSize = 12.sp, fontWeight = FontWeight.Medium)
                }

                val isActive = user.status.uppercase() == "ACTIVO"
                val statusBg = if (isActive) colors.successBg else colors.errorBg
                val statusTextColor = if (isActive) colors.successText else colors.error
                val statusIcon = if (isActive) Icons.Default.CheckCircle else Icons.Default.Cancel

                Row(
                    modifier = Modifier
                        .background(statusBg, RoundedCornerShape(20.dp))
                        .padding(horizontal = 14.dp, vertical = 6.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    Icon(
                        statusIcon,
                        contentDescription = null,
                        tint = statusTextColor,
                        modifier = Modifier.size(14.dp)
                    )
                    Text(
                        text = if (isActive) "Activo" else "Inactivo",
                        color = statusTextColor,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Bold
                    )
                }
            }

            Spacer(modifier = Modifier.height(28.dp))

            // Tarjeta de Información solo lectura
            Card(
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = colors.card),
                border = BorderStroke(1.dp, colors.border),
                elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 0.dp else 2.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    InfoItem(
                        icon = Icons.Default.Badge,
                        label = "Número de Documento",
                        value = user.documento,
                        fontFamily = FontFamily.Monospace
                    )
                    HorizontalDivider(
                        color = colors.border,
                        modifier = Modifier.padding(vertical = 12.dp)
                    )
                    InfoItem(
                        icon = Icons.Default.Person,
                        label = "Nombre",
                        value = user.nombre
                    )
                    HorizontalDivider(
                        color = colors.border,
                        modifier = Modifier.padding(vertical = 12.dp)
                    )
                    InfoItem(
                        icon = Icons.Default.School,
                        label = "Grado",
                        value = user.grado
                    )
                }
            }

            Spacer(modifier = Modifier.height(20.dp))

            // Tarjeta de Ayuda/Soporte
            Card(
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = colors.card),
                border = BorderStroke(1.dp, colors.border),
                elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 0.dp else 2.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable {
                        val intent = Intent(Intent.ACTION_VIEW, Uri.parse("https://wahl-mirai.lat/"))
                        context.startActivity(intent)
                    }
            ) {
                Row(
                    modifier = Modifier.padding(16.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(
                        Icons.Default.Info,
                        contentDescription = "Info",
                        tint = colors.accentText,
                        modifier = Modifier.size(24.dp)
                    )
                    Spacer(modifier = Modifier.width(14.dp))
                    Text(
                        text = "Si necesitas modificar información o ayuda, accede a https://wahl-mirai.lat/",
                        color = colors.text,
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Medium
                    )
                }
            }

            Spacer(modifier = Modifier.height(28.dp))

            // Botón Cerrar Sesión
            Button(
                onClick = {
                    viewModel.cerrarSesion()
                    onCerrarSesion()
                },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(52.dp),
                shape = RoundedCornerShape(10.dp),
                colors = ButtonDefaults.buttonColors(containerColor = colors.primaryContainer)
            ) {
                Icon(Icons.AutoMirrored.Filled.ExitToApp, contentDescription = null, modifier = Modifier.size(20.dp), tint = Color.White)
                Spacer(modifier = Modifier.width(8.dp))
                Text("Cerrar Sesión", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = Color.White)
            }

            Spacer(modifier = Modifier.height(20.dp))

            ThemeToggleSwitch(
                isDarkTheme = isDarkTheme,
                onToggleTheme = onToggleTheme
            )

            Spacer(modifier = Modifier.height(24.dp))
        }
    }
}

@Composable
fun InfoItem(
    icon: ImageVector,
    label: String,
    value: String,
    fontFamily: FontFamily = FontFamily.SansSerif
) {
    val colors = WahlTheme.colors
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            icon,
            contentDescription = null,
            tint = colors.accentText,
            modifier = Modifier.size(22.dp)
        )
        Spacer(modifier = Modifier.width(14.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(label, color = colors.subtext, fontSize = 12.sp)
            Text(
                text = value,
                color = colors.text,
                fontSize = 15.sp,
                fontWeight = FontWeight.SemiBold,
                fontFamily = fontFamily
            )
        }
    }
}
