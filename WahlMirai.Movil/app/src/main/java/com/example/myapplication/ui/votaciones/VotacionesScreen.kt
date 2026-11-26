package com.example.myapplication.ui.votaciones

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.R
import com.example.myapplication.data.models.Candidate
import com.example.myapplication.data.models.VotacionStatus
import com.example.myapplication.data.models.VotingItem
import com.example.myapplication.ui.components.ThemeToggleSwitch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun VotacionesScreen(
    viewModel: VotacionesViewModel,
    isDarkTheme: Boolean = true,
    onToggleTheme: () -> Unit = {},
    onPerfilClick: () -> Unit = {},
    onNavigateToDetalle: (String) -> Unit = {}
) {
    val uiState by viewModel.uiState.collectAsState()

    LaunchedEffect(Unit) {
        viewModel.cargarVotaciones()
    }

    val containerBg = if (isDarkTheme) Color(0xFF0B1220) else Color(0xFFF8FAFC)
    val cardBg = if (isDarkTheme) Color(0xFF111B2E) else Color(0xFFFFFFFF)
    val textColor = if (isDarkTheme) Color.White else Color(0xFF0F172A)
    val subtitleColor = if (isDarkTheme) Color.LightGray else Color(0xFF64748B)

    val snackbarHostState = remember { SnackbarHostState() }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(stringResource(id = R.string.title_votaciones), fontWeight = FontWeight.Bold, color = textColor) },
                actions = {
                    IconButton(
                        onClick = onPerfilClick,
                        modifier = Modifier.padding(end = 4.dp)
                    ) {
                        Icon(
                            imageVector = Icons.Default.AccountCircle,
                            contentDescription = "Perfil de usuario",
                            tint = if (isDarkTheme) Color(0xFF60A5FA) else Color(0xFF2563EB),
                            modifier = Modifier.size(32.dp)
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = containerBg,
                    titleContentColor = textColor,
                    actionIconContentColor = textColor
                )
            )
        },
        containerColor = containerBg
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 16.dp)
        ) {
            Text(
                text = stringResource(id = R.string.subtitle_votaciones),
                color = subtitleColor,
                fontSize = 14.sp,
                modifier = Modifier.padding(bottom = 16.dp)
            )

            // Tabs / Filtros (Activas / Ya participaste)
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(bottom = 16.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                FiltroChip(
                    text = stringResource(id = R.string.tab_activas),
                    icon = Icons.Default.BarChart,
                    isSelected = uiState.filtroActual == FiltroVotaciones.ACTIVAS,
                    isDarkTheme = isDarkTheme,
                    onClick = { viewModel.setFiltro(FiltroVotaciones.ACTIVAS) }
                )
                FiltroChip(
                    text = stringResource(id = R.string.tab_ya_participaste),
                    icon = Icons.Default.CheckCircle,
                    isSelected = uiState.filtroActual == FiltroVotaciones.YA_PARTICIPASTE,
                    isDarkTheme = isDarkTheme,
                    onClick = { viewModel.setFiltro(FiltroVotaciones.YA_PARTICIPASTE) }
                )
            }

            // Lista de Votaciones filtradas dinámicamente
            val votacionesFiltradas = when (uiState.filtroActual) {
                FiltroVotaciones.ACTIVAS -> uiState.votaciones.filter {
                    (it.status == VotacionStatus.ACTIVA ||
                        it.status == VotacionStatus.PROGRAMADA ||
                        it.status == VotacionStatus.INSCRIPCION ||
                        it.status == VotacionStatus.PROPUESTAS) && !it.hasVoted
                }
                FiltroVotaciones.YA_PARTICIPASTE -> uiState.votaciones.filter {
                    it.hasVoted || it.status == VotacionStatus.FINALIZADA
                }
            }

            if (uiState.isLoading && uiState.votaciones.isEmpty()) {
                Box(
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxWidth(),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator(color = Color(0xFF3B82F6))
                }
            } else if (uiState.errorMessage != null && uiState.votaciones.isEmpty()) {
                Box(
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxWidth(),
                    contentAlignment = Alignment.Center
                ) {
                    Card(
                        shape = RoundedCornerShape(16.dp),
                        colors = CardDefaults.cardColors(containerColor = cardBg),
                        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp),
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Column(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(vertical = 32.dp, horizontal = 24.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Icon(
                                imageVector = Icons.Default.CloudOff,
                                contentDescription = null,
                                tint = Color(0xFFEF4444),
                                modifier = Modifier.size(48.dp)
                            )
                            Spacer(modifier = Modifier.height(12.dp))
                            Text(
                                text = uiState.errorMessage ?: "Error al cargar",
                                color = textColor,
                                fontSize = 15.sp,
                                textAlign = TextAlign.Center
                            )
                            Spacer(modifier = Modifier.height(16.dp))
                            Button(
                                onClick = { viewModel.cargarVotaciones() },
                                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF2563EB))
                            ) {
                                Text("Reintentar", color = Color.White)
                            }
                        }
                    }
                }
            } else if (votacionesFiltradas.isEmpty()) {
                Box(
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxWidth(),
                    contentAlignment = Alignment.Center
                ) {
                    Card(
                        shape = RoundedCornerShape(16.dp),
                        colors = CardDefaults.cardColors(containerColor = cardBg),
                        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp),
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Column(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(vertical = 40.dp, horizontal = 24.dp),
                            horizontalAlignment = Alignment.CenterHorizontally,
                            verticalArrangement = Arrangement.Center
                        ) {
                            Icon(
                                imageVector = Icons.Default.HowToVote,
                                contentDescription = null,
                                tint = subtitleColor,
                                modifier = Modifier.size(56.dp)
                            )
                            Spacer(modifier = Modifier.height(16.dp))
                            Text(
                                text = if (uiState.filtroActual == FiltroVotaciones.ACTIVAS)
                                    "No hay votaciones activas"
                                else
                                    "No has participado en ninguna votación.",
                                color = textColor,
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Medium,
                                textAlign = TextAlign.Center
                            )
                        }
                    }
                }
            } else {
                LazyColumn(
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxWidth(),
                    verticalArrangement = Arrangement.spacedBy(16.dp),
                    contentPadding = PaddingValues(bottom = 16.dp)
                ) {
                    items(votacionesFiltradas, key = { it.id }) { votacion ->
                        VotacionCard(
                            votacion = votacion,
                            cardBg = cardBg,
                            textColor = textColor,
                            onCardClick = {
                                if (votacion.status == VotacionStatus.ACTIVA ||
                                    votacion.status == VotacionStatus.PROPUESTAS ||
                                    votacion.hasVoted ||
                                    votacion.status == VotacionStatus.FINALIZADA
                                ) {
                                    onNavigateToDetalle(votacion.id)
                                }
                            }
                        )
                    }
                }
            }

            // Botón Switch Elegante de Tema (Sol / Luna)
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 12.dp),
                contentAlignment = Alignment.Center
            ) {
                ThemeToggleSwitch(
                    isDarkTheme = isDarkTheme,
                    onToggleTheme = onToggleTheme
                )
            }
        }
    }
}

@Composable
fun FiltroChip(
    text: String,
    icon: ImageVector,
    isSelected: Boolean,
    isDarkTheme: Boolean,
    onClick: () -> Unit
) {
    val backgroundColor by animateColorAsState(
        targetValue = if (isSelected) Color(0xFF3B82F6) else if (isDarkTheme) Color(0xFF172338) else Color(0xFFE2E8F0),
        label = "chipBg"
    )
    val contentColor by animateColorAsState(
        targetValue = if (isSelected) Color.White else if (isDarkTheme) Color.LightGray else Color(0xFF475569),
        label = "chipContent"
    )

    Row(
        modifier = Modifier
            .background(backgroundColor, RoundedCornerShape(20.dp))
            .border(1.dp, if (isSelected) Color.Transparent else Color(0xFF334155), RoundedCornerShape(20.dp))
            .clickable { onClick() }
            .padding(horizontal = 16.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Icon(icon, contentDescription = null, tint = contentColor, modifier = Modifier.size(18.dp))
        Text(text, color = contentColor, fontSize = 14.sp, fontWeight = FontWeight.Medium)
    }
}

@Composable
fun VotacionCard(
    votacion: VotingItem,
    cardBg: Color,
    textColor: Color,
    onCardClick: () -> Unit
) {
    Card(
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = cardBg),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
        modifier = Modifier
            .fillMaxWidth()
            .clickable { onCardClick() }
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            // Header: Estado y Tiempo
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(
                    modifier = Modifier
                        .background(
                            if (votacion.hasVoted) Color(0xFF10B981)
                            else if (votacion.status == VotacionStatus.ACTIVA) Color(0xFF2563EB)
                            else Color(0xFF475569),
                            RoundedCornerShape(8.dp)
                        )
                        .padding(horizontal = 10.dp, vertical = 4.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    Icon(
                        imageVector = if (votacion.hasVoted) Icons.Default.CheckCircle 
                                      else if (votacion.status == VotacionStatus.ACTIVA) Icons.Default.FlashOn 
                                      else if (votacion.status == VotacionStatus.PROGRAMADA ||
                                          votacion.status == VotacionStatus.INSCRIPCION ||
                                          votacion.status == VotacionStatus.PROPUESTAS) Icons.Default.Schedule
                                      else Icons.Default.Check,
                        contentDescription = null,
                        tint = Color.White,
                        modifier = Modifier.size(14.dp)
                    )
                    Text(
                        text = when {
                            votacion.hasVoted -> stringResource(id = R.string.status_ya_voto)
                            votacion.status == VotacionStatus.ACTIVA -> stringResource(id = R.string.status_en_curso)
                            votacion.status == VotacionStatus.PROGRAMADA -> stringResource(id = R.string.status_programada)
                            votacion.status == VotacionStatus.INSCRIPCION -> "Inscripción"
                            votacion.status == VotacionStatus.PROPUESTAS -> "Propuestas"
                            else -> stringResource(id = R.string.status_finalizada)
                        },
                        color = Color.White,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Bold
                    )
                }

                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    Icon(
                        imageVector = if (votacion.status == VotacionStatus.FINALIZADA) Icons.Default.Event else Icons.Default.Schedule,
                        contentDescription = null,
                        tint = Color.Gray,
                        modifier = Modifier.size(14.dp)
                    )
                    Text(
                        text = votacion.timeDisplay,
                        color = Color.Gray,
                        fontSize = 12.sp
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Pregunta
            Text(
                text = votacion.title,
                color = textColor,
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                lineHeight = 24.sp
            )

            Spacer(modifier = Modifier.height(16.dp))

            // Opciones
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                votacion.candidates.forEach { candidate ->
                    CandidateProgressItem(
                        candidate = candidate,
                        textColor = textColor
                    )
                }
            }

            // Botón Votar / Ya Votaste
            Spacer(modifier = Modifier.height(20.dp))
            Button(
                onClick = onCardClick,
                enabled = votacion.status == VotacionStatus.ACTIVA,
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = if (votacion.hasVoted) Color(0xFF059669) else Color(0xFF3B82F6),
                    disabledContainerColor = if (votacion.hasVoted) Color(0xFF065F46) else Color(0xFF334155)
                ),
                modifier = Modifier
                    .fillMaxWidth()
                    .height(48.dp)
            ) {
                Icon(
                    imageVector = if (votacion.hasVoted) Icons.Default.CheckCircle else Icons.Default.HowToVote,
                    contentDescription = null,
                    modifier = Modifier.size(18.dp),
                    tint = Color.White
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = if (votacion.hasVoted) stringResource(id = R.string.status_ya_voto) else stringResource(id = R.string.btn_votar),
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold,
                    color = Color.White
                )
            }
        }
    }
}

@Composable
fun CandidateProgressItem(
    candidate: Candidate,
    textColor: Color
) {
    val colorActivo = Color(0xFF3B82F6)

    val animatedProgress by animateFloatAsState(
        targetValue = candidate.percentage,
        animationSpec = tween(durationMillis = 600, easing = FastOutSlowInEasing),
        label = "progressBar"
    )

    Column(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                modifier = Modifier
                    .size(12.dp)
                    .background(colorActivo, CircleShape)
            )

            Spacer(modifier = Modifier.width(12.dp))

            Text(
                text = candidate.name,
                color = textColor,
                fontSize = 14.sp,
                modifier = Modifier.weight(1f)
            )

            Text(
                text = "${(animatedProgress * 100).toInt()}% (${candidate.votes} votos)",
                color = textColor,
                fontSize = 14.sp,
                fontWeight = FontWeight.Bold
            )
        }

        Spacer(modifier = Modifier.height(6.dp))

        Box(
            modifier = Modifier
                .fillMaxWidth()
                .padding(start = 24.dp)
                .height(6.dp)
                .background(Color(0xFF1E293B), RoundedCornerShape(3.dp))
        ) {
            Box(
                modifier = Modifier
                    .fillMaxWidth(animatedProgress.coerceIn(0.01f, 1.0f))
                    .fillMaxHeight()
                    .background(colorActivo, RoundedCornerShape(3.dp))
            )
        }
    }
}
