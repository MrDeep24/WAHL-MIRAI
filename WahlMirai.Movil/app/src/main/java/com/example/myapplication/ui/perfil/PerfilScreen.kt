package com.example.myapplication.ui.perfil

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.myapplication.ui.components.ThemeToggleSwitch
import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.clickable
import androidx.compose.ui.platform.LocalContext

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

    val containerBg = if (isDarkTheme) Color(0xFF0B1220) else Color(0xFFF8FAFC)
    val cardBg = if (isDarkTheme) Color(0xFF111B2E) else Color(0xFFFFFFFF)
    val textColor = if (isDarkTheme) Color.White else Color(0xFF0F172A)
    val navIconBg = if (isDarkTheme) Color(0xFF172338) else Color(0xFFE2E8F0)
    val buttonBg = if (isDarkTheme) Color(0xFF1E3A8A) else Color(0xFF2563EB)

    Scaffold(
        topBar = {
            CenterAlignedTopAppBar(
                title = { 
                    Text(
                        text = "Perfil de Usuario",
                        fontWeight = FontWeight.Bold,
                        color = textColor
                    )
                },
                navigationIcon = {
                    IconButton(
                        onClick = onBack,
                        modifier = Modifier
                            .padding(start = 8.dp)
                            .background(navIconBg, CircleShape)
                    ) {
                        Icon(
                            Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = "Volver",
                            tint = if (isDarkTheme) Color(0xFF60A5FA) else Color(0xFF2563EB)
                        )
                    }
                },
                actions = {
                    Spacer(modifier = Modifier.width(56.dp))
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = containerBg,
                    titleContentColor = textColor
                )
            )
        },
        containerColor = containerBg
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(24.dp))

            // Avatar estático
            Surface(
                shape = RoundedCornerShape(24.dp),
                color = Color.White,
                shadowElevation = 6.dp,
                modifier = Modifier.size(100.dp)
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Text(
                        text = user.nombre.firstOrNull()?.toString()?.uppercase() ?: "W",
                        color = Color(0xFF0F2B5B),
                        fontWeight = FontWeight.Bold,
                        fontSize = 48.sp
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = user.nombre,
                color = textColor,
                fontSize = 24.sp,
                fontWeight = FontWeight.Bold
            )
            
            Spacer(modifier = Modifier.height(8.dp))
            
            // Etiquetas de Rol y Estado
            Row(
                horizontalArrangement = Arrangement.spacedBy(10.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Chip de Rol
                Row(
                    modifier = Modifier
                        .background(if (isDarkTheme) Color(0xFF172338) else Color(0xFFE2E8F0), RoundedCornerShape(16.dp))
                        .padding(horizontal = 14.dp, vertical = 6.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    Icon(
                        Icons.Default.Person,
                        contentDescription = null,
                        tint = if (isDarkTheme) Color.LightGray else Color.DarkGray,
                        modifier = Modifier.size(14.dp)
                    )
                    Text(user.rol, color = textColor, fontSize = 12.sp)
                }

                // Chip de Estado (Activo / Inactivo)
                val isActive = user.status.uppercase() == "ACTIVO"
                val statusBg = if (isDarkTheme) {
                    if (isActive) Color(0xFF064E3B) else Color(0xFF7F1D1D)
                } else {
                    if (isActive) Color(0xFFD1FAE5) else Color(0xFFFEE2E2)
                }
                val statusTextColor = if (isDarkTheme) {
                    if (isActive) Color(0xFF34D399) else Color(0xFFF87171)
                } else {
                    if (isActive) Color(0xFF065F46) else Color(0xFF991B1B)
                }
                val statusIcon = if (isActive) Icons.Default.CheckCircle else Icons.Default.Cancel

                Row(
                    modifier = Modifier
                        .background(statusBg, RoundedCornerShape(16.dp))
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

            Spacer(modifier = Modifier.height(32.dp))

            // Tarjeta de Información solo lectura
            Card(
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = cardBg),
                elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    InfoItem(
                        icon = Icons.Default.Badge,
                        label = "Número de Documento",
                        value = user.documento,
                        textColor = textColor
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    InfoItem(
                        icon = Icons.Default.Person,
                        label = "Nombre",
                        value = user.nombre,
                        textColor = textColor
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    InfoItem(
                        icon = Icons.Default.School,
                        label = "Grado",
                        value = user.grado,
                        textColor = textColor
                    )
                }
            }

            Spacer(modifier = Modifier.weight(1f))

            // Tarjeta de Ayuda/Soporte
            val context = LocalContext.current
            Card(
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = cardBg),
                elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable {
                        val intent = Intent(Intent.ACTION_VIEW, Uri.parse("http://wahl-mirai.lat/"))
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
                        tint = Color(0xFF60A5FA),
                        modifier = Modifier.size(24.dp)
                    )
                    Spacer(modifier = Modifier.width(16.dp))
                    Text(
                        text = "Si necesitas modificar información o ayuda, accede a http://wahl-mirai.lat/",
                        color = textColor,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Medium
                    )
                }
            }

            Spacer(modifier = Modifier.weight(1f))

            // Botón Cerrar Sesión
            Button(
                onClick = {
                    viewModel.cerrarSesion()
                    onCerrarSesion()
                },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(56.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = buttonBg)
            ) {
                Icon(Icons.AutoMirrored.Filled.ExitToApp, contentDescription = null, modifier = Modifier.size(20.dp), tint = Color.White)
                Spacer(modifier = Modifier.width(8.dp))
                Text("Cerrar Sesión", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = Color.White)
            }
            
            Spacer(modifier = Modifier.height(24.dp))
            
            // Botón Switch Elegante de Tema (Sol / Luna)
            ThemeToggleSwitch(
                isDarkTheme = isDarkTheme,
                onToggleTheme = onToggleTheme
            )
            
            Spacer(modifier = Modifier.height(32.dp))
        }
    }
}

@Composable
fun InfoItem(
    icon: ImageVector,
    label: String,
    value: String,
    textColor: Color = Color.White
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            icon,
            contentDescription = null,
            tint = Color(0xFF60A5FA),
            modifier = Modifier.size(24.dp)
        )
        Spacer(modifier = Modifier.width(16.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(label, color = Color(0xFF94A3B8), fontSize = 12.sp)
            Text(value, color = textColor, fontSize = 16.sp, fontWeight = FontWeight.Medium)
        }
    }
}
