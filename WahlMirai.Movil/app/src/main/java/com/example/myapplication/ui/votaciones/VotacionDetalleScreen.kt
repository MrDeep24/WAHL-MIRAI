package com.example.myapplication.ui.votaciones

import android.app.Activity
import android.view.WindowManager
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Article
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.EmojiEvents
import androidx.compose.material.icons.filled.HowToVote
import androidx.compose.material.icons.filled.VerifiedUser
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.R
import com.example.myapplication.data.models.Candidate
import com.example.myapplication.data.models.VotacionStatus
import com.example.myapplication.ui.components.CandidateAvatar
import com.example.myapplication.ui.theme.CandidateChartPalette
import com.example.myapplication.ui.theme.WahlTheme
import com.example.myapplication.ui.theme.WhiteVoteNeutralColor
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun VotacionDetalleScreen(
    viewModel: VotacionDetalleViewModel,
    votacionId: String,
    preselectedCandidateId: String? = null,
    isDarkTheme: Boolean = true,
    onBack: () -> Unit,
    onNavigateToPlan: (String, String) -> Unit,
    onVoteSuccess: () -> Unit
) {
    val uiState by viewModel.uiState.collectAsState()
    val colors = WahlTheme.colors
    val context = LocalContext.current

    // FLAG_SECURE para evitar captura/grabación de pantalla por privacidad de voto
    val activity = context as? Activity
    DisposableEffect(Unit) {
        activity?.window?.setFlags(
            WindowManager.LayoutParams.FLAG_SECURE,
            WindowManager.LayoutParams.FLAG_SECURE
        )
        onDispose {
            activity?.window?.clearFlags(WindowManager.LayoutParams.FLAG_SECURE)
        }
    }

    LaunchedEffect(votacionId, preselectedCandidateId) {
        viewModel.loadVotingItem(votacionId, preselectedCandidateId)
    }

    DisposableEffect(votacionId) {
        onDispose {
            viewModel.stopLiveUpdates()
        }
    }

    val item = uiState.item
    val selectedCandidate = item?.candidates?.find { it.id == uiState.selectedCandidateId }

    // Comprobante de participación tras voto exitoso (RN-3: sin revelar el candidato elegido)
    if (uiState.voteSuccess && item != null) {
        val timestamp = remember {
            SimpleDateFormat("dd/MM/yyyy • HH:mm", Locale.getDefault()).format(Date())
        }
        AlertDialog(
            onDismissRequest = onVoteSuccess,
            icon = {
                Surface(
                    shape = CircleShape,
                    color = colors.successBg,
                    modifier = Modifier.size(64.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Icon(
                            imageVector = Icons.Default.CheckCircle,
                            contentDescription = null,
                            tint = colors.confirm,
                            modifier = Modifier.size(38.dp)
                        )
                    }
                }
            },
            title = {
                Text(
                    text = "¡Voto registrado!",
                    fontWeight = FontWeight.Bold,
                    color = colors.text,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.fillMaxWidth()
                )
            },
            text = {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(10.dp),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(
                        text = item.title,
                        color = colors.text,
                        fontSize = 15.sp,
                        fontWeight = FontWeight.SemiBold,
                        textAlign = TextAlign.Center
                    )
                    Surface(
                        shape = RoundedCornerShape(10.dp),
                        color = colors.surfaceLow
                    ) {
                        Row(
                            modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(6.dp)
                        ) {
                            Icon(
                                imageVector = Icons.Default.VerifiedUser,
                                contentDescription = null,
                                tint = colors.confirm,
                                modifier = Modifier.size(16.dp)
                            )
                            Text(
                                text = "Participación confirmada • $timestamp",
                                color = colors.subtext,
                                fontSize = 12.sp
                            )
                        }
                    }
                    Text(
                        text = "Tu voto fue almacenado de forma anónima, secreta y definitiva.",
                        color = colors.subtext,
                        fontSize = 13.sp,
                        textAlign = TextAlign.Center
                    )
                }
            },
            confirmButton = {
                Button(
                    onClick = onVoteSuccess,
                    shape = RoundedCornerShape(10.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = colors.confirm),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Volver a votaciones", color = Color.White, fontWeight = FontWeight.Bold)
                }
            },
            containerColor = colors.card,
            titleContentColor = colors.text,
            textContentColor = colors.text
        )
    }

    // Diálogo de Confirmación de Voto Definitivo (Botón verde semántico según DESIGN.md)
    if (uiState.showConfirmDialog && selectedCandidate != null) {
        AlertDialog(
            onDismissRequest = { if (!uiState.isSubmitting) viewModel.closeConfirmDialog() },
            title = {
                Text(
                    text = "Confirmar tu voto",
                    fontWeight = FontWeight.Bold,
                    color = colors.text
                )
            },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    Text(
                        text = "Estás a punto de emitir tu voto definitivo por:",
                        color = colors.subtext,
                        fontSize = 14.sp
                    )
                    Surface(
                        shape = RoundedCornerShape(12.dp),
                        color = colors.surfaceLow,
                        border = BorderStroke(1.dp, colors.primary.copy(alpha = 0.4f)),
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Row(
                            modifier = Modifier.padding(12.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(12.dp)
                        ) {
                            CandidateAvatar(
                                name = selectedCandidate.name,
                                number = selectedCandidate.number,
                                photoUrl = selectedCandidate.photoUrl,
                                isWhiteVote = selectedCandidate.isWhiteVote,
                                size = 44.dp,
                                showNumberBadge = false
                            )
                            Column {
                                Text(
                                    text = selectedCandidate.name,
                                    color = colors.text,
                                    fontSize = 15.sp,
                                    fontWeight = FontWeight.Bold
                                )
                                if (!selectedCandidate.isWhiteVote) {
                                    Text(
                                        text = "N° ${selectedCandidate.number}",
                                        color = colors.accentText,
                                        fontFamily = FontFamily.Monospace,
                                        fontSize = 12.sp,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                }
                            }
                        }
                    }
                    Text(
                        text = stringResource(id = R.string.confirm_msg),
                        color = colors.error,
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Medium
                    )
                }
            },
            confirmButton = {
                Button(
                    onClick = { viewModel.submitVote(onSuccess = {}) },
                    enabled = !uiState.isSubmitting,
                    shape = RoundedCornerShape(10.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = colors.confirm)
                ) {
                    if (uiState.isSubmitting) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(18.dp),
                            color = Color.White,
                            strokeWidth = 2.dp
                        )
                    } else {
                        Text(
                            text = stringResource(id = R.string.btn_confirmar),
                            color = Color.White,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }
            },
            dismissButton = {
                OutlinedButton(
                    onClick = { viewModel.closeConfirmDialog() },
                    enabled = !uiState.isSubmitting,
                    shape = RoundedCornerShape(10.dp),
                    border = BorderStroke(1.dp, colors.border)
                ) {
                    Text(stringResource(id = R.string.btn_cancelar), color = colors.subtext)
                }
            },
            containerColor = colors.card,
            titleContentColor = colors.text,
            textContentColor = colors.text
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = item?.title ?: stringResource(id = R.string.title_detalle),
                        fontWeight = FontWeight.Bold,
                        color = colors.text,
                        maxLines = 1
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
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = colors.background.copy(alpha = 0.94f),
                    titleContentColor = colors.text
                )
            )
        },
        containerColor = Color.Transparent
    ) { innerPadding ->
        if (uiState.isLoading || item == null) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = colors.primary)
            }
        } else {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding)
                    .padding(horizontal = 16.dp)
            ) {
                val isTopicElection = item.electionType.equals("TEMAS", ignoreCase = true)
                val subtitleText = when {
                    item.hasVoted -> "Ya registraste tu voto. Resultados actuales:"
                    item.status == VotacionStatus.FINALIZADA -> "Resultados finales del proceso:"
                    item.status == VotacionStatus.PROPUESTAS -> "Consulta las propuestas antes de la votación:"
                    item.status != VotacionStatus.ACTIVA -> "Esta votación aún no está habilitada para votar."
                    isTopicElection -> "Selecciona una opción para votar:"
                    else -> "Selecciona tu candidato para votar:"
                }
                if (!item.description.isNullOrBlank()) {
                    Text(
                        text = item.description,
                        color = colors.text,
                        fontSize = 14.sp,
                        modifier = Modifier.padding(top = 6.dp, bottom = 4.dp)
                    )
                }
                Text(
                    text = subtitleText,
                    color = colors.subtext,
                    fontSize = 14.sp,
                    modifier = Modifier.padding(vertical = 8.dp)
                )

                val showResults = item.hasVoted || item.status == VotacionStatus.FINALIZADA
                val winnerSummary = remember(item.candidates, item.status) { item.winnerSummary() }

                if (showResults) {
                    ElectionResultsBanner(
                        summary = winnerSummary,
                        isLive = item.status == VotacionStatus.ACTIVA
                    )
                    Spacer(modifier = Modifier.height(10.dp))
                }

                val orderedCandidates = if (showResults) {
                    item.candidates.sortedByDescending { it.votes }
                } else {
                    item.candidates
                }

                // Lista de Candidatos / Opciones
                LazyColumn(
                    modifier = Modifier.weight(1f),
                    verticalArrangement = Arrangement.spacedBy(12.dp),
                    contentPadding = PaddingValues(bottom = 8.dp)
                ) {
                    itemsIndexed(orderedCandidates, key = { _, candidate -> candidate.id }) { index, candidate ->
                        val barColor = if (candidate.isWhiteVote) {
                            WhiteVoteNeutralColor
                        } else {
                            CandidateChartPalette[index % CandidateChartPalette.size]
                        }
                        val isWinner = winnerSummary.winner?.id == candidate.id
                        val isTiedTop = winnerSummary.isTie &&
                            winnerSummary.topCandidate?.votes == candidate.votes &&
                            candidate.votes > 0
                        CandidateSelectionCard(
                            candidate = candidate,
                            barColor = barColor,
                            isTopicElection = isTopicElection,
                            isSelected = candidate.id == uiState.selectedCandidateId,
                            isLocked = item.hasVoted || item.status != VotacionStatus.ACTIVA,
                            showResults = showResults,
                            isWinner = isWinner,
                            isTiedTop = isTiedTop,
                            onSelect = { viewModel.selectCandidate(candidate.id) },
                            onVerPlan = { onNavigateToPlan(item.id, candidate.id) }
                        )
                    }
                }

                uiState.errorMessage?.let { errorMsg ->
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = errorMsg,
                        color = colors.error,
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Medium
                    )
                }

                // Botón Votar o Ya Votó
                Spacer(modifier = Modifier.height(14.dp))
                val canVote = !item.hasVoted && item.status == VotacionStatus.ACTIVA
                Button(
                    onClick = { viewModel.openConfirmDialog() },
                    enabled = canVote && uiState.selectedCandidateId != null && !uiState.isSubmitting,
                    shape = RoundedCornerShape(10.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = if (item.hasVoted) colors.confirm else colors.primaryContainer,
                        disabledContainerColor = if (item.hasVoted) colors.confirm else colors.surfaceHigh,
                        disabledContentColor = if (item.hasVoted) Color.White else colors.subtext
                    ),
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(52.dp)
                ) {
                    Icon(
                        imageVector = if (item.hasVoted) Icons.Default.CheckCircle else Icons.Default.HowToVote,
                        contentDescription = null,
                        tint = if (item.hasVoted || (canVote && uiState.selectedCandidateId != null)) Color.White else colors.subtext,
                        modifier = Modifier.size(20.dp)
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Text(
                        text = when {
                            item.hasVoted -> stringResource(id = R.string.status_ya_voto)
                            item.status == VotacionStatus.FINALIZADA -> stringResource(id = R.string.status_finalizada)
                            item.status != VotacionStatus.ACTIVA -> "Etapa de ${item.status.name.lowercase()}"
                            uiState.selectedCandidateId == null -> "Selecciona una opción para votar"
                            else -> stringResource(id = R.string.btn_votar)
                        },
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold,
                        color = if (item.hasVoted || (canVote && uiState.selectedCandidateId != null)) Color.White else colors.subtext
                    )
                }
                Spacer(modifier = Modifier.height(16.dp))
            }
        }
    }
}

@Composable
fun ElectionResultsBanner(
    summary: com.example.myapplication.data.models.ElectionWinnerSummary,
    isLive: Boolean
) {
    val colors = WahlTheme.colors
    val containerColor: Color
    val borderColor: Color
    val accentColor: Color
    val headline: String
    val title: String
    val subtitle: String

    when {
        summary.isFinalized && summary.winner != null -> {
            val pct = if (summary.totalVotes > 0) {
                ((summary.winner.votes * 1000f) / summary.totalVotes).toInt() / 10f
            } else 0f
            val majorityLabel = if (summary.isAbsoluteMajority) "Mayoría Absoluta (50% + 1)" else "Mayoría Simple"
            containerColor = colors.successBg
            borderColor = colors.confirm.copy(alpha = 0.45f)
            accentColor = colors.successText
            headline = "GANADOR DEFINITIVO • $majorityLabel"
            title = summary.winner.name
            subtitle = "${summary.winner.votes} voto(s) ($pct% de ${summary.totalVotes} emitidos)"
        }
        summary.isFinalized && summary.isTie && summary.topCandidate != null -> {
            containerColor = if (colors.isDark) Color(0xFF451A03) else Color(0xFFFEF3C7)
            borderColor = Color(0xFFF59E0B).copy(alpha = 0.55f)
            accentColor = if (colors.isDark) Color(0xFFFCD34D) else Color(0xFFB45309)
            headline = "EMPATE TÉCNICO (RN-17)"
            title = "Empate en el primer lugar"
            subtitle = "Dos o más opciones obtuvieron ${summary.topCandidate.votes} voto(s) de ${summary.totalVotes} emitidos."
        }
        summary.isFinalized -> {
            containerColor = colors.surfaceLow
            borderColor = colors.border
            accentColor = colors.subtext
            headline = "ELECCIÓN FINALIZADA"
            title = "Sin votos registrados"
            subtitle = "No se emitieron votos en este proceso electoral."
        }
        else -> {
            containerColor = colors.surfaceLow
            borderColor = colors.primary.copy(alpha = 0.35f)
            accentColor = colors.accentText
            headline = if (isLive) "● EN VIVO • ESCRUTINIO EN TIEMPO REAL" else "ESCRUTINIO PARCIAL"
            title = if (summary.topCandidate != null && summary.totalVotes > 0) {
                "Lidera parcialmente: ${summary.topCandidate.name}"
            } else {
                "Total emitidos: ${summary.totalVotes} voto(s)"
            }
            subtitle = "Total: ${summary.totalVotes} voto(s) • El ganador oficial se define al finalizar."
        }
    }

    Surface(
        shape = RoundedCornerShape(14.dp),
        color = containerColor,
        border = BorderStroke(1.dp, borderColor),
        modifier = Modifier.fillMaxWidth()
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Surface(
                shape = CircleShape,
                color = accentColor.copy(alpha = 0.16f),
                modifier = Modifier.size(44.dp)
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Icon(
                        imageVector = Icons.Default.EmojiEvents,
                        contentDescription = null,
                        tint = accentColor,
                        modifier = Modifier.size(24.dp)
                    )
                }
            }
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = headline,
                    color = accentColor,
                    fontSize = 11.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = title,
                    color = colors.text,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = subtitle,
                    color = colors.subtext,
                    fontSize = 12.sp
                )
            }
        }
    }
}

@Composable
fun CandidateSelectionCard(
    candidate: Candidate,
    barColor: Color,
    isTopicElection: Boolean = false,
    isSelected: Boolean,
    isLocked: Boolean,
    showResults: Boolean = isLocked,
    isWinner: Boolean = false,
    isTiedTop: Boolean = false,
    onSelect: () -> Unit,
    onVerPlan: () -> Unit
) {
    val colors = WahlTheme.colors
    val cardContainer = when {
        isWinner -> colors.successBg.copy(alpha = 0.55f)
        isSelected -> colors.primarySoft
        candidate.isWhiteVote -> colors.surfaceLow
        else -> colors.card
    }
    val borderColor = when {
        isWinner -> colors.confirm
        isSelected -> colors.primary
        candidate.isWhiteVote -> WhiteVoteNeutralColor.copy(alpha = 0.4f)
        else -> colors.border
    }

    Card(
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = cardContainer),
        border = BorderStroke(if (isSelected || isWinner) 2.dp else 1.dp, borderColor),
        elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 0.dp else 2.dp),
        modifier = Modifier
            .fillMaxWidth()
            .clickable(enabled = !isLocked, onClick = onSelect)
    ) {
        Column(modifier = Modifier.padding(14.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                RadioButton(
                    selected = isSelected,
                    onClick = if (!isLocked) onSelect else null,
                    enabled = !isLocked,
                    colors = RadioButtonDefaults.colors(
                        selectedColor = colors.primary,
                        unselectedColor = colors.subtext
                    )
                )

                Spacer(modifier = Modifier.width(4.dp))

                CandidateAvatar(
                    name = candidate.name,
                    number = candidate.number,
                    photoUrl = candidate.photoUrl,
                    isWhiteVote = candidate.isWhiteVote,
                    size = 50.dp,
                    showNumberBadge = !isTopicElection
                )

                Spacer(modifier = Modifier.width(12.dp))

                Column(modifier = Modifier.weight(1f)) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Text(
                            text = candidate.name,
                            color = colors.text,
                            fontSize = 16.sp,
                            fontWeight = FontWeight.Bold,
                            modifier = Modifier.weight(1f, fill = false)
                        )
                        if (isWinner) {
                            Surface(
                                shape = RoundedCornerShape(20.dp),
                                color = colors.confirm
                            ) {
                                Text(
                                    text = "Ganador",
                                    color = Color.White,
                                    fontSize = 10.sp,
                                    fontWeight = FontWeight.Bold,
                                    modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp)
                                )
                            }
                        } else if (isTiedTop) {
                            Surface(
                                shape = RoundedCornerShape(20.dp),
                                color = Color(0xFFD97706)
                            ) {
                                Text(
                                    text = "Empate",
                                    color = Color.White,
                                    fontSize = 10.sp,
                                    fontWeight = FontWeight.Bold,
                                    modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp)
                                )
                            }
                        } else if (isSelected) {
                            Surface(
                                shape = RoundedCornerShape(20.dp),
                                color = colors.primary
                            ) {
                                Text(
                                    text = "Elegido",
                                    color = Color.White,
                                    fontSize = 10.sp,
                                    fontWeight = FontWeight.Bold,
                                    modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp)
                                )
                            }
                        }
                    }

                    if (candidate.isWhiteVote) {
                        Text(
                            text = "Opción institucional de neutralidad",
                            color = colors.subtext,
                            fontSize = 12.sp
                        )
                    } else if (!isTopicElection) {
                        Text(
                            text = "N° ${candidate.number} • Grado ${candidate.grade}",
                            color = colors.accentText,
                            fontFamily = FontFamily.Monospace,
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold
                        )
                    }
                }
            }

            val hasPlanInfo = candidate.slogan.isNotBlank() ||
                candidate.proposals.isNotEmpty() ||
                !candidate.pdfUrl.isNullOrBlank()
            if (!candidate.isWhiteVote && !isTopicElection && hasPlanInfo) {
                if (candidate.slogan.isNotBlank()) {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = "\"${candidate.slogan}\"",
                        color = colors.subtext,
                        fontSize = 13.sp,
                        modifier = Modifier.padding(start = 40.dp)
                    )
                }

                Spacer(modifier = Modifier.height(8.dp))

                OutlinedButton(
                    onClick = onVerPlan,
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.outlinedButtonColors(contentColor = colors.accentText),
                    border = BorderStroke(1.dp, colors.primary),
                    modifier = Modifier.padding(start = 40.dp)
                ) {
                    Icon(Icons.AutoMirrored.Filled.Article, contentDescription = null, modifier = Modifier.size(16.dp))
                    Spacer(modifier = Modifier.width(6.dp))
                    Text(stringResource(id = R.string.btn_ver_plan), fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                }
            } else if (!candidate.isWhiteVote && isTopicElection && candidate.slogan.isNotBlank()) {
                Spacer(modifier = Modifier.height(6.dp))
                Text(
                    text = candidate.slogan,
                    color = colors.subtext,
                    fontSize = 13.sp,
                    modifier = Modifier.padding(start = 40.dp)
                )
            }

            // Resultados diferenciados por color
            if (showResults) {
                Spacer(modifier = Modifier.height(12.dp))
                val animatedProgress by animateFloatAsState(
                    targetValue = candidate.percentage,
                    animationSpec = tween(durationMillis = 600, easing = FastOutSlowInEasing),
                    label = "detailProgress"
                )
                val percentageInt = (animatedProgress * 100).toInt()
                Column(modifier = Modifier.padding(start = 40.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = "Resultados:",
                            color = colors.subtext,
                            fontSize = 12.sp
                        )
                        Text(
                            text = "$percentageInt% (${candidate.votes} votos)",
                            color = barColor,
                            fontSize = 12.sp,
                            fontWeight = FontWeight.Bold
                        )
                    }
                    Spacer(modifier = Modifier.height(5.dp))
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(7.dp)
                            .clip(RoundedCornerShape(4.dp))
                            .background(colors.progressTrack)
                    ) {
                        if (animatedProgress > 0f) {
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth(animatedProgress.coerceIn(0f, 1f))
                                    .fillMaxHeight()
                                    .background(barColor, RoundedCornerShape(4.dp))
                            )
                        }
                    }
                }
            }
        }
    }
}
