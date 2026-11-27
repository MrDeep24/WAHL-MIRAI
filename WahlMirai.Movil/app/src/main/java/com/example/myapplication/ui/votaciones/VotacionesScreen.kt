package com.example.myapplication.ui.votaciones

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.BorderStroke
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
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.R
import com.example.myapplication.data.models.Candidate
import com.example.myapplication.data.models.VotacionStatus
import com.example.myapplication.data.models.VotingItem
import com.example.myapplication.ui.theme.CandidateChartPalette
import com.example.myapplication.ui.theme.WahlTheme
import com.example.myapplication.ui.theme.WhiteVoteNeutralColor

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
    val colors = WahlTheme.colors

    LaunchedEffect(Unit) {
        viewModel.cargarVotaciones()
    }

    DisposableEffect(Unit) {
        viewModel.startLiveUpdates()
        onDispose {
            viewModel.stopLiveUpdates()
        }
    }

    val snackbarHostState = remember { SnackbarHostState() }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = stringResource(id = R.string.title_votaciones),
                            fontWeight = FontWeight.Bold,
                            color = colors.text
                        )
                    }
                },
                actions = {
                    IconButton(
                        onClick = onToggleTheme,
                        modifier = Modifier
                            .padding(end = 4.dp)
                            .background(colors.surfaceLow, CircleShape)
                            .size(38.dp)
                    ) {
                        Icon(
                            imageVector = if (isDarkTheme) Icons.Default.LightMode else Icons.Default.DarkMode,
                            contentDescription = if (isDarkTheme) "Cambiar a modo claro" else "Cambiar a modo oscuro",
                            tint = if (isDarkTheme) Color(0xFFFBBF24) else colors.primaryContainer,
                            modifier = Modifier.size(20.dp)
                        )
                    }
                    IconButton(
                        onClick = onPerfilClick,
                        modifier = Modifier.padding(end = 8.dp)
                    ) {
                        Icon(
                            imageVector = Icons.Default.AccountCircle,
                            contentDescription = "Perfil de usuario",
                            tint = colors.accentText,
                            modifier = Modifier.size(32.dp)
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = colors.background.copy(alpha = 0.94f),
                    titleContentColor = colors.text,
                    actionIconContentColor = colors.text
                )
            )
        },
        containerColor = Color.Transparent
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 16.dp)
        ) {
            Text(
                text = stringResource(id = R.string.subtitle_votaciones),
                color = colors.subtext,
                fontSize = 14.sp,
                modifier = Modifier.padding(bottom = 14.dp)
            )

            // Tabs / Filtros (Activas / Ya participaste)
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(bottom = 16.dp),
                horizontalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                FiltroChip(
                    text = stringResource(id = R.string.tab_activas),
                    icon = Icons.Default.HowToVote,
                    isSelected = uiState.filtroActual == FiltroVotaciones.ACTIVAS,
                    onClick = { viewModel.setFiltro(FiltroVotaciones.ACTIVAS) }
                )
                FiltroChip(
                    text = stringResource(id = R.string.tab_ya_participaste),
                    icon = Icons.Default.CheckCircle,
                    isSelected = uiState.filtroActual == FiltroVotaciones.YA_PARTICIPASTE,
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

            PullToRefreshBox(
                isRefreshing = uiState.isLoading && uiState.votaciones.isNotEmpty(),
                onRefresh = { viewModel.cargarVotaciones() },
                modifier = Modifier
                    .weight(1f)
                    .fillMaxWidth()
            ) {
                if (uiState.isLoading && uiState.votaciones.isEmpty()) {
                    Column(
                        modifier = Modifier.fillMaxSize(),
                        verticalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        repeat(3) {
                            VotacionSkeletonCard()
                        }
                    }
                } else if (uiState.errorMessage != null && uiState.votaciones.isEmpty()) {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Card(
                            shape = RoundedCornerShape(16.dp),
                            colors = CardDefaults.cardColors(containerColor = colors.card),
                            border = BorderStroke(1.dp, colors.border),
                            elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 0.dp else 2.dp),
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
                                    tint = colors.error,
                                    modifier = Modifier.size(48.dp)
                                )
                                Spacer(modifier = Modifier.height(12.dp))
                                Text(
                                    text = uiState.errorMessage ?: "Error al cargar",
                                    color = colors.text,
                                    fontSize = 15.sp,
                                    textAlign = TextAlign.Center
                                )
                                Spacer(modifier = Modifier.height(16.dp))
                                Button(
                                    onClick = { viewModel.cargarVotaciones() },
                                    shape = RoundedCornerShape(10.dp),
                                    colors = ButtonDefaults.buttonColors(containerColor = colors.primaryContainer)
                                ) {
                                    Text("Reintentar", color = Color.White, fontWeight = FontWeight.SemiBold)
                                }
                            }
                        }
                    }
                } else if (votacionesFiltradas.isEmpty()) {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Card(
                            shape = RoundedCornerShape(16.dp),
                            colors = CardDefaults.cardColors(containerColor = colors.card),
                            border = BorderStroke(1.dp, colors.border),
                            elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 0.dp else 2.dp),
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
                                    tint = colors.subtext,
                                    modifier = Modifier.size(56.dp)
                                )
                                Spacer(modifier = Modifier.height(16.dp))
                                Text(
                                    text = if (uiState.filtroActual == FiltroVotaciones.ACTIVAS)
                                        "No hay votaciones activas"
                                    else
                                        "No has participado en ninguna votación.",
                                    color = colors.text,
                                    fontSize = 16.sp,
                                    fontWeight = FontWeight.Medium,
                                    textAlign = TextAlign.Center
                                )
                            }
                        }
                    }
                } else {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        verticalArrangement = Arrangement.spacedBy(16.dp),
                        contentPadding = PaddingValues(bottom = 20.dp)
                    ) {
                        items(votacionesFiltradas, key = { it.id }) { votacion ->
                            VotacionCard(
                                votacion = votacion,
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
            }
        }
    }
}

@Composable
fun VotacionSkeletonCard() {
    val colors = WahlTheme.colors
    Card(
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = colors.card),
        border = BorderStroke(1.dp, colors.border),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Box(
                    modifier = Modifier
                        .width(96.dp)
                        .height(24.dp)
                        .clip(RoundedCornerShape(8.dp))
                        .background(colors.surfaceLow)
                )
                Box(
                    modifier = Modifier
                        .width(110.dp)
                        .height(18.dp)
                        .clip(RoundedCornerShape(6.dp))
                        .background(colors.surfaceLow)
                )
            }
            Box(
                modifier = Modifier
                    .fillMaxWidth(0.78f)
                    .height(22.dp)
                    .clip(RoundedCornerShape(6.dp))
                    .background(colors.surfaceLow)
            )
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(44.dp)
                    .clip(RoundedCornerShape(10.dp))
                    .background(colors.surfaceLow)
            )
        }
    }
}

@Composable
fun FiltroChip(
    text: String,
    icon: ImageVector,
    isSelected: Boolean,
    onClick: () -> Unit
) {
    val colors = WahlTheme.colors
    val backgroundColor by animateColorAsState(
        targetValue = if (isSelected) colors.primaryContainer else colors.surfaceLow,
        label = "chipBg"
    )
    val contentColor by animateColorAsState(
        targetValue = if (isSelected) Color.White else colors.subtext,
        label = "chipContent"
    )

    Row(
        modifier = Modifier
            .clip(RoundedCornerShape(20.dp))
            .background(backgroundColor)
            .border(1.dp, if (isSelected) Color.Transparent else colors.border, RoundedCornerShape(20.dp))
            .clickable { onClick() }
            .padding(horizontal = 16.dp, vertical = 9.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Icon(icon, contentDescription = null, tint = contentColor, modifier = Modifier.size(18.dp))
        Text(text, color = contentColor, fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
    }
}

@Composable
fun VotacionCard(
    votacion: VotingItem,
    onCardClick: () -> Unit
) {
    val colors = WahlTheme.colors
    val canOpenDetail = votacion.status == VotacionStatus.ACTIVA ||
        votacion.status == VotacionStatus.PROPUESTAS ||
        votacion.hasVoted ||
        votacion.status == VotacionStatus.FINALIZADA

    Card(
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = colors.card),
        border = BorderStroke(1.dp, colors.border),
        elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 2.dp else 3.dp),
        modifier = Modifier
            .fillMaxWidth()
            .clickable(enabled = canOpenDetail) { onCardClick() }
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            // Header: Estado y Tiempo
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                val badgeBg = when {
                    votacion.hasVoted -> colors.confirm
                    votacion.status == VotacionStatus.ACTIVA -> colors.primary
                    votacion.status == VotacionStatus.PROPUESTAS -> Color(0xFF0891B2)
                    else -> WhiteVoteNeutralColor
                }
                Row(
                    modifier = Modifier
                        .background(badgeBg, RoundedCornerShape(20.dp))
                        .padding(horizontal = 10.dp, vertical = 4.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    Icon(
                        imageVector = if (votacion.hasVoted) Icons.Default.CheckCircle
                        else if (votacion.status == VotacionStatus.ACTIVA) Icons.Default.FlashOn
                        else if (votacion.status == VotacionStatus.PROGRAMADA ||
                            votacion.status == VotacionStatus.INSCRIPCION ||
                            votacion.status == VotacionStatus.PROPUESTAS
                        ) Icons.Default.Schedule
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
                        tint = colors.subtext,
                        modifier = Modifier.size(14.dp)
                    )
                    Text(
                        text = votacion.timeDisplay,
                        color = colors.subtext,
                        fontSize = 12.sp
                    )
                }
            }

            Spacer(modifier = Modifier.height(14.dp))

            // Título y descripción corta
            Text(
                text = votacion.title,
                color = colors.text,
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                lineHeight = 24.sp
            )

            if (!votacion.description.isNullOrBlank()) {
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = votacion.description,
                    color = colors.subtext,
                    fontSize = 13.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
            }

            Spacer(modifier = Modifier.height(14.dp))

            // Banner de Ganador / Empate / Escrutinio cuando aplica
            val showResults = votacion.hasVoted || votacion.status == VotacionStatus.FINALIZADA
            val summary = remember(votacion.candidates, votacion.status) { votacion.winnerSummary() }

            if (summary.isFinalized) {
                val (bannerBg, bannerBorder, bannerAccent, bannerText) = when {
                    summary.winner != null -> {
                        val pct = if (summary.totalVotes > 0) {
                            ((summary.winner.votes * 100f) / summary.totalVotes).toInt()
                        } else 0
                        listOf(
                            colors.successBg,
                            colors.confirm.copy(alpha = 0.4f),
                            colors.successText,
                            "Ganador: ${summary.winner.name} • ${summary.winner.votes} votos ($pct%)"
                        )
                    }
                    summary.isTie && summary.topCandidate != null -> listOf(
                        if (colors.isDark) Color(0xFF451A03) else Color(0xFFFEF3C7),
                        Color(0xFFF59E0B).copy(alpha = 0.5f),
                        if (colors.isDark) Color(0xFFFCD34D) else Color(0xFFB45309),
                        "Empate técnico en 1° lugar (${summary.topCandidate.votes} votos)"
                    )
                    else -> listOf(
                        colors.surfaceLow,
                        colors.border,
                        colors.subtext,
                        "Proceso finalizado sin votos registrados"
                    )
                }
                Surface(
                    shape = RoundedCornerShape(12.dp),
                    color = bannerBg as Color,
                    border = BorderStroke(1.dp, bannerBorder as Color),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Row(
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 9.dp),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Icon(
                            imageVector = Icons.Default.EmojiEvents,
                            contentDescription = null,
                            tint = bannerAccent as Color,
                            modifier = Modifier.size(18.dp)
                        )
                        Text(
                            text = bannerText as String,
                            color = bannerAccent,
                            fontSize = 12.sp,
                            fontWeight = FontWeight.Bold,
                            maxLines = 2,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                }
                Spacer(modifier = Modifier.height(12.dp))
            }

            // Opciones (ordenadas por votos si se muestran resultados; compactas si hay muchas antes de votar)
            val orderedCandidates = if (showResults) {
                votacion.candidates.sortedByDescending { it.votes }
            } else {
                votacion.candidates
            }
            val displayedCandidates = if (orderedCandidates.size > 4) {
                orderedCandidates.take(4)
            } else {
                orderedCandidates
            }
            val hiddenCount = (orderedCandidates.size - displayedCandidates.size).coerceAtLeast(0)

            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                displayedCandidates.forEachIndexed { index, candidate ->
                    val barColor = if (candidate.isWhiteVote) {
                        WhiteVoteNeutralColor
                    } else {
                        CandidateChartPalette[index % CandidateChartPalette.size]
                    }
                    CandidateProgressItem(
                        candidate = candidate,
                        barColor = barColor,
                        showResults = showResults
                    )
                }
                if (hiddenCount > 0) {
                    Text(
                        text = "+$hiddenCount ${if (hiddenCount == 1) "opción adicional" else "opciones adicionales"}",
                        color = colors.accentText,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.SemiBold,
                        modifier = Modifier.padding(start = 24.dp)
                    )
                }
                if (showResults) {
                    Text(
                        text = "Total emitidos: ${summary.totalVotes} voto(s)",
                        color = colors.subtext,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Medium,
                        modifier = Modifier.padding(start = 22.dp, top = 2.dp)
                    )
                }
            }

            // Botón Votar / Ver propuestas / Ver resultados
            Spacer(modifier = Modifier.height(18.dp))
            Button(
                onClick = onCardClick,
                enabled = canOpenDetail,
                shape = RoundedCornerShape(10.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = when {
                        votacion.hasVoted -> colors.confirm
                        votacion.status == VotacionStatus.ACTIVA -> colors.primary
                        else -> colors.primaryContainer
                    },
                    disabledContainerColor = colors.surfaceHigh,
                    disabledContentColor = colors.subtext
                ),
                modifier = Modifier
                    .fillMaxWidth()
                    .height(46.dp)
            ) {
                Icon(
                    imageVector = when {
                        votacion.hasVoted -> Icons.Default.CheckCircle
                        votacion.status == VotacionStatus.FINALIZADA -> Icons.Default.BarChart
                        votacion.status == VotacionStatus.PROPUESTAS -> Icons.Default.Visibility
                        else -> Icons.Default.HowToVote
                    },
                    contentDescription = null,
                    modifier = Modifier.size(18.dp),
                    tint = if (canOpenDetail) Color.White else colors.subtext
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = when {
                        votacion.hasVoted -> stringResource(id = R.string.status_ya_voto)
                        votacion.status == VotacionStatus.FINALIZADA -> "Ver resultados"
                        votacion.status == VotacionStatus.PROPUESTAS -> "Ver propuestas"
                        votacion.status == VotacionStatus.ACTIVA -> stringResource(id = R.string.btn_votar)
                        else -> "Próximamente"
                    },
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Bold,
                    color = if (canOpenDetail) Color.White else colors.subtext
                )
            }
        }
    }
}

@Composable
fun CandidateProgressItem(
    candidate: Candidate,
    barColor: Color,
    showResults: Boolean
) {
    val colors = WahlTheme.colors
    val animatedProgress by animateFloatAsState(
        targetValue = if (showResults) candidate.percentage else 0f,
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
                    .size(10.dp)
                    .background(barColor, CircleShape)
            )

            Spacer(modifier = Modifier.width(12.dp))

            Text(
                text = candidate.name,
                color = colors.text,
                fontSize = 14.sp,
                modifier = Modifier.weight(1f),
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )

            if (showResults) {
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = "${(animatedProgress * 100).toInt()}% (${candidate.votes})",
                    color = colors.text,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        if (showResults) {
            Spacer(modifier = Modifier.height(6.dp))

            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(start = 22.dp)
                    .height(6.dp)
                    .clip(RoundedCornerShape(3.dp))
                    .background(colors.progressTrack)
            ) {
                if (animatedProgress > 0f) {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth(animatedProgress.coerceIn(0f, 1f))
                            .fillMaxHeight()
                            .background(barColor, RoundedCornerShape(3.dp))
                    )
                }
            }
        }
    }
}
