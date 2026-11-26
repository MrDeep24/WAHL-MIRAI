package com.example.myapplication.ui.votaciones

import android.app.Activity
import android.view.WindowManager
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Article
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.HowToVote
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.R
import com.example.myapplication.data.models.Candidate

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

    val containerBg = if (isDarkTheme) Color(0xFF0B1220) else Color(0xFFF8FAFC)
    val cardBg = if (isDarkTheme) Color(0xFF111B2E) else Color(0xFFFFFFFF)
    val textColor = if (isDarkTheme) Color.White else Color(0xFF0F172A)
    val navIconBg = if (isDarkTheme) Color(0xFF172338) else Color(0xFFE2E8F0)

    val item = uiState.item
    val selectedCandidate = item?.candidates?.find { it.id == uiState.selectedCandidateId }

    // Diálogo de Confirmación de Voto Definitivo
    if (uiState.showConfirmDialog && selectedCandidate != null) {
        AlertDialog(
            onDismissRequest = { if (!uiState.isSubmitting) viewModel.closeConfirmDialog() },
            title = {
                Text(
                    text = stringResource(id = R.string.confirm_title, selectedCandidate.name),
                    fontWeight = FontWeight.Bold,
                    color = textColor
                )
            },
            text = {
                Text(
                    text = stringResource(id = R.string.confirm_msg),
                    color = Color(0xFFEF4444),
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Medium
                )
            },
            confirmButton = {
                Button(
                    onClick = {
                        viewModel.submitVote(onSuccess = onVoteSuccess)
                    },
                    enabled = !uiState.isSubmitting,
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF2563EB))
                ) {
                    if (uiState.isSubmitting) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(18.dp),
                            color = Color.White,
                            strokeWidth = 2.dp
                        )
                    } else {
                        Text(stringResource(id = R.string.btn_confirmar), color = Color.White)
                    }
                }
            },
            dismissButton = {
                TextButton(
                    onClick = { viewModel.closeConfirmDialog() },
                    enabled = !uiState.isSubmitting
                ) {
                    Text(stringResource(id = R.string.btn_cancelar), color = if (isDarkTheme) Color.LightGray else Color.DarkGray)
                }
            },
            containerColor = cardBg,
            titleContentColor = textColor,
            textContentColor = textColor
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(id = R.string.title_detalle), fontWeight = FontWeight.Bold, color = textColor) },
                navigationIcon = {
                    IconButton(
                        onClick = onBack,
                        modifier = Modifier.padding(start = 8.dp).background(navIconBg, CircleShape)
                    ) {
                        Icon(
                            Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = "Volver",
                            tint = if (isDarkTheme) Color(0xFF60A5FA) else Color(0xFF2563EB)
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = containerBg,
                    titleContentColor = textColor
                )
            )
        },
        containerColor = containerBg
    ) { innerPadding ->
        if (uiState.isLoading || item == null) {
            Box(
                modifier = Modifier.fillMaxSize().padding(innerPadding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = Color(0xFF3B82F6))
            }
        } else {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding)
                    .padding(horizontal = 16.dp)
            ) {
                Text(
                    text = if (item.hasVoted) "Selección registrada previamente:" else "Selecciona tu candidato u opción:",
                    color = if (isDarkTheme) Color.LightGray else Color(0xFF64748B),
                    fontSize = 14.sp,
                    modifier = Modifier.padding(vertical = 8.dp)
                )

                // Lista de Candidatos / Opciones
                LazyColumn(
                    modifier = Modifier.weight(1f),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    items(item.candidates, key = { it.id }) { candidate ->
                        CandidateSelectionCard(
                            candidate = candidate,
                            isSelected = candidate.id == uiState.selectedCandidateId,
                            isLocked = item.hasVoted || item.status != com.example.myapplication.data.models.VotacionStatus.ACTIVA,
                            showResults = item.hasVoted || item.status == com.example.myapplication.data.models.VotacionStatus.FINALIZADA,
                            cardBg = cardBg,
                            textColor = textColor,
                            onSelect = { viewModel.selectCandidate(candidate.id) },
                            onVerPlan = { onNavigateToPlan(item.id, candidate.id) }
                        )
                    }
                }

                uiState.errorMessage?.let { errorMsg ->
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = errorMsg,
                        color = Color(0xFFEF4444),
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Medium
                    )
                }

                // Botón Votar o Ya Votó
                Spacer(modifier = Modifier.height(16.dp))
                val canVote = !item.hasVoted && item.status == com.example.myapplication.data.models.VotacionStatus.ACTIVA
                Button(
                    onClick = { viewModel.openConfirmDialog() },
                    enabled = canVote && uiState.selectedCandidateId != null && !uiState.isSubmitting,
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = if (item.hasVoted) Color(0xFF059669) else Color(0xFF2563EB),
                        disabledContainerColor = if (item.hasVoted) Color(0xFF065F46) else Color(0xFF334155)
                    ),
                    modifier = Modifier.fillMaxWidth().height(52.dp)
                ) {
                    Icon(
                        imageVector = if (item.hasVoted) Icons.Default.CheckCircle else Icons.Default.HowToVote,
                        contentDescription = null,
                        tint = Color.White,
                        modifier = Modifier.size(20.dp)
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Text(
                        text = when {
                            item.hasVoted -> stringResource(id = R.string.status_ya_voto)
                            item.status == com.example.myapplication.data.models.VotacionStatus.FINALIZADA -> stringResource(id = R.string.status_finalizada)
                            item.status != com.example.myapplication.data.models.VotacionStatus.ACTIVA -> "Etapa de ${item.status.name.lowercase()}"
                            else -> stringResource(id = R.string.btn_votar)
                        },
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold,
                        color = Color.White
                    )
                }
                Spacer(modifier = Modifier.height(16.dp))
            }
        }
    }
}

@Composable
fun CandidateSelectionCard(
    candidate: Candidate,
    isSelected: Boolean,
    isLocked: Boolean,
    showResults: Boolean = isLocked,
    cardBg: Color,
    textColor: Color,
    onSelect: () -> Unit,
    onVerPlan: () -> Unit
) {
    val borderColor = if (isSelected) Color(0xFF3B82F6) else Color.Transparent

    Card(
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = cardBg),
        modifier = Modifier
            .fillMaxWidth()
            .border(2.dp, borderColor, RoundedCornerShape(14.dp))
            .clickable(enabled = !isLocked, onClick = onSelect)
    ) {
        Column(modifier = Modifier.padding(14.dp)) {
            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {
                RadioButton(
                    selected = isSelected,
                    onClick = if (!isLocked) onSelect else null,
                    enabled = !isLocked,
                    colors = RadioButtonDefaults.colors(
                        selectedColor = Color(0xFF3B82F6),
                        unselectedColor = Color(0xFF64748B)
                    )
                )
                Spacer(modifier = Modifier.width(8.dp))
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = candidate.name,
                        color = textColor,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold
                    )
                    if (!candidate.isWhiteVote) {
                        Text(
                            text = "N° ${candidate.number} • Grado ${candidate.grade}",
                            color = Color(0xFF3B82F6),
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold
                        )
                    }
                }
            }

            if (!candidate.isWhiteVote && candidate.slogan.isNotBlank()) {
                Spacer(modifier = Modifier.height(6.dp))
                Text(
                    text = "\"${candidate.slogan}\"",
                    color = Color(0xFF94A3B8),
                    fontSize = 13.sp,
                    modifier = Modifier.padding(start = 40.dp)
                )

                Spacer(modifier = Modifier.height(8.dp))

                OutlinedButton(
                    onClick = onVerPlan,
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.outlinedButtonColors(contentColor = Color(0xFF60A5FA)),
                    border = ButtonDefaults.outlinedButtonBorder.copy(brush = SolidColor(Color(0xFF3B82F6))),
                    modifier = Modifier.padding(start = 40.dp)
                ) {
                    Icon(Icons.AutoMirrored.Filled.Article, contentDescription = null, modifier = Modifier.size(16.dp))
                    Spacer(modifier = Modifier.width(6.dp))
                    Text(stringResource(id = R.string.btn_ver_plan), fontSize = 12.sp)
                }
            }

            // Si ya votó o la votación finalizó, muestra los votos y barra de progreso en tiempo real
            if (showResults) {
                Spacer(modifier = Modifier.height(12.dp))
                val percentageInt = (candidate.percentage * 100).toInt()
                Column(modifier = Modifier.padding(start = 40.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = "Resultados en tiempo real:",
                            color = Color(0xFF94A3B8),
                            fontSize = 12.sp
                        )
                        Text(
                            text = "$percentageInt% (${candidate.votes} votos)",
                            color = Color(0xFF3B82F6),
                            fontSize = 12.sp,
                            fontWeight = FontWeight.Bold
                        )
                    }
                    Spacer(modifier = Modifier.height(4.dp))
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(6.dp)
                            .background(Color(0xFF1E293B), RoundedCornerShape(3.dp))
                    ) {
                        Box(
                            modifier = Modifier
                                .fillMaxWidth(candidate.percentage.coerceIn(0.01f, 1.0f))
                                .fillMaxHeight()
                                .background(Color(0xFF3B82F6), RoundedCornerShape(3.dp))
                        )
                    }
                }
            }
        }
    }
}
