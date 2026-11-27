package com.example.myapplication.ui.plan

import android.app.Activity
import android.view.WindowManager
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.OpenInNew
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.R
import com.example.myapplication.data.models.VotacionStatus
import com.example.myapplication.ui.components.CandidateAvatar
import com.example.myapplication.ui.theme.WahlTheme

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PlanGobiernoScreen(
    viewModel: PlanGobiernoViewModel,
    votacionId: String,
    candidateId: String,
    isDarkTheme: Boolean = true,
    onBack: () -> Unit,
    onVotarPorCandidato: (String, String) -> Unit
) {
    val uiState by viewModel.uiState.collectAsState()
    val colors = WahlTheme.colors
    val context = LocalContext.current

    // FLAG_SECURE para seguridad en información electoral
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

    LaunchedEffect(votacionId, candidateId) {
        viewModel.loadPlan(votacionId, candidateId)
    }

    val candidate = uiState.candidate
    val item = uiState.item

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = stringResource(id = R.string.title_plan_gobierno),
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
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = colors.background.copy(alpha = 0.94f),
                    titleContentColor = colors.text
                )
            )
        },
        bottomBar = {
            if (candidate != null && item != null && !item.hasVoted && item.status == VotacionStatus.ACTIVA) {
                Surface(
                    color = colors.background.copy(alpha = 0.95f),
                    shadowElevation = 8.dp,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Box(
                        modifier = Modifier
                            .padding(horizontal = 20.dp, vertical = 14.dp)
                            .navigationBarsPadding()
                    ) {
                        Button(
                            onClick = { onVotarPorCandidato(votacionId, candidateId) },
                            shape = RoundedCornerShape(10.dp),
                            colors = ButtonDefaults.buttonColors(containerColor = colors.primaryContainer),
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(52.dp)
                        ) {
                            Icon(
                                Icons.Default.HowToVote,
                                contentDescription = null,
                                tint = Color.White,
                                modifier = Modifier.size(20.dp)
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = stringResource(id = R.string.btn_votar_por, candidate.name),
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Bold,
                                color = Color.White
                            )
                        }
                    }
                }
            }
        },
        containerColor = Color.Transparent
    ) { innerPadding ->
        if (uiState.isLoading || candidate == null || item == null) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = colors.primary)
            }
        } else {
            LazyColumn(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding)
                    .padding(horizontal = 20.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp),
                contentPadding = PaddingValues(top = 12.dp, bottom = 24.dp)
            ) {
                // 1. Header Hero del Candidato con foto o iniciales
                item {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 12.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        CandidateAvatar(
                            name = candidate.name,
                            number = candidate.number,
                            photoUrl = candidate.photoUrl,
                            isWhiteVote = candidate.isWhiteVote,
                            size = 96.dp,
                            shape = CircleShape,
                            showNumberBadge = true
                        )

                        Spacer(modifier = Modifier.height(16.dp))

                        Text(
                            text = candidate.name,
                            color = colors.text,
                            fontSize = 24.sp,
                            fontWeight = FontWeight.Bold,
                            textAlign = TextAlign.Center
                        )

                        Spacer(modifier = Modifier.height(4.dp))

                        Text(
                            text = "Candidato N° ${candidate.number} • Grado ${candidate.grade}",
                            color = colors.accentText,
                            fontFamily = FontFamily.Monospace,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.SemiBold
                        )

                        if (candidate.slogan.isNotBlank()) {
                            Spacer(modifier = Modifier.height(12.dp))
                            Surface(
                                shape = RoundedCornerShape(16.dp),
                                color = colors.surfaceLow,
                                border = BorderStroke(1.dp, colors.border)
                            ) {
                                Text(
                                    text = "\"${candidate.slogan}\"",
                                    color = colors.subtext,
                                    fontSize = 14.sp,
                                    fontWeight = FontWeight.Medium,
                                    textAlign = TextAlign.Center,
                                    modifier = Modifier.padding(horizontal = 18.dp, vertical = 10.dp)
                                )
                            }
                        }
                    }
                }

                // 2. Lector de PDF (Plan de Gobierno oficial - Clic para abrir / desplegar)
                item {
                    var isPdfExpanded by remember(candidate.id) {
                        mutableStateOf(!candidate.pdfUrl.isNullOrBlank())
                    }
                    var isPdfLoading by remember(candidate.id) { mutableStateOf(false) }
                    var isPdfSizeValid by remember(candidate.id) { mutableStateOf(true) }
                    var isUploadedDoc by remember(candidate.id) { mutableStateOf(false) }
                    var pdfBitmaps by remember(candidate.id) {
                        mutableStateOf<List<android.graphics.Bitmap>>(emptyList())
                    }

                    LaunchedEffect(candidate.id, isPdfExpanded) {
                        if (isPdfExpanded && pdfBitmaps.isEmpty() && !isPdfLoading) {
                            isPdfLoading = true
                            val loaded = PdfHelper.loadCandidatePlanDocument(context, candidate)
                            isPdfSizeValid = loaded.isSizeValid
                            isUploadedDoc = loaded.isUploadedDocument
                            pdfBitmaps = loaded.bitmaps
                            isPdfLoading = false
                        }
                    }

                    Card(
                        shape = RoundedCornerShape(16.dp),
                        colors = CardDefaults.cardColors(containerColor = colors.card),
                        border = BorderStroke(1.dp, colors.border),
                        elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 0.dp else 2.dp),
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable { isPdfExpanded = !isPdfExpanded }
                    ) {
                        Column(modifier = Modifier.padding(18.dp)) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.SpaceBetween
                            ) {
                                Row(
                                    verticalAlignment = Alignment.CenterVertically,
                                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                                ) {
                                    Surface(
                                        shape = CircleShape,
                                        color = colors.errorBg,
                                        modifier = Modifier.size(42.dp)
                                    ) {
                                        Box(contentAlignment = Alignment.Center) {
                                            Icon(
                                                imageVector = Icons.Default.PictureAsPdf,
                                                contentDescription = "PDF Plan de Gobierno",
                                                tint = colors.error,
                                                modifier = Modifier.size(22.dp)
                                            )
                                        }
                                    }
                                    Column {
                                        Text(
                                            text = "Plan de Gobierno (PDF)",
                                            color = colors.accentText,
                                            fontSize = 15.sp,
                                            fontWeight = FontWeight.Bold
                                        )
                                        Text(
                                            text = when {
                                                pdfBitmaps.isNotEmpty() && isUploadedDoc ->
                                                    "Documento subido por el candidato (${pdfBitmaps.size} pág.)"
                                                pdfBitmaps.isNotEmpty() ->
                                                    "Resumen de propuestas (${pdfBitmaps.size} pág.)"
                                                !candidate.pdfUrl.isNullOrBlank() ->
                                                    "Documento oficial del candidato"
                                                else -> "plan_gobierno.pdf"
                                            },
                                            color = colors.subtext,
                                            fontSize = 12.sp
                                        )
                                    }
                                }

                                IconButton(onClick = { isPdfExpanded = !isPdfExpanded }) {
                                    Icon(
                                        imageVector = if (isPdfExpanded) Icons.Default.KeyboardArrowUp else Icons.AutoMirrored.Filled.OpenInNew,
                                        contentDescription = "Abrir PDF",
                                        tint = colors.accentText
                                    )
                                }
                            }

                            Spacer(modifier = Modifier.height(12.dp))

                            OutlinedButton(
                                onClick = { isPdfExpanded = !isPdfExpanded },
                                shape = RoundedCornerShape(10.dp),
                                border = BorderStroke(1.dp, colors.primary),
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Icon(
                                    imageVector = if (isPdfExpanded) Icons.Default.VisibilityOff else Icons.Default.Visibility,
                                    contentDescription = null,
                                    tint = colors.accentText,
                                    modifier = Modifier.size(18.dp)
                                )
                                Spacer(modifier = Modifier.width(8.dp))
                                Text(
                                    text = if (isPdfExpanded) "Ocultar documento PDF" else "Seleccionar para abrir PDF completo",
                                    color = colors.accentText,
                                    fontWeight = FontWeight.SemiBold,
                                    fontSize = 14.sp
                                )
                            }

                            if (isPdfExpanded) {
                                Spacer(modifier = Modifier.height(16.dp))

                                if (!isPdfSizeValid) {
                                    Box(
                                        modifier = Modifier
                                            .fillMaxWidth()
                                            .background(colors.errorBg, RoundedCornerShape(12.dp))
                                            .padding(16.dp),
                                        contentAlignment = Alignment.Center
                                    ) {
                                        Row(
                                            verticalAlignment = Alignment.CenterVertically,
                                            horizontalArrangement = Arrangement.spacedBy(10.dp)
                                        ) {
                                            Icon(
                                                imageVector = Icons.Default.Warning,
                                                contentDescription = null,
                                                tint = colors.error,
                                                modifier = Modifier.size(24.dp)
                                            )
                                            Text(
                                                text = "El archivo PDF excede el tamaño máximo permitido de 10 MB.",
                                                color = colors.error,
                                                fontSize = 13.sp,
                                                fontWeight = FontWeight.Medium
                                            )
                                        }
                                    }
                                } else if (pdfBitmaps.isNotEmpty()) {
                                    Column(
                                        modifier = Modifier
                                            .fillMaxWidth()
                                            .background(colors.surfaceLow, RoundedCornerShape(12.dp))
                                            .padding(12.dp),
                                        horizontalAlignment = Alignment.CenterHorizontally,
                                        verticalArrangement = Arrangement.spacedBy(12.dp)
                                    ) {
                                        pdfBitmaps.forEach { bitmap ->
                                            Image(
                                                bitmap = bitmap.asImageBitmap(),
                                                contentDescription = "Página del Plan de Gobierno PDF",
                                                modifier = Modifier
                                                    .fillMaxWidth()
                                                    .clip(RoundedCornerShape(8.dp))
                                            )
                                        }
                                    }
                                } else {
                                    Box(
                                        modifier = Modifier
                                            .fillMaxWidth()
                                            .height(100.dp)
                                            .background(colors.surfaceLow, RoundedCornerShape(12.dp)),
                                        contentAlignment = Alignment.Center
                                    ) {
                                        Text(
                                            text = "Cargando documento PDF...",
                                            color = colors.subtext,
                                            fontSize = 14.sp
                                        )
                                    }
                                }
                            }
                        }
                    }
                }

                // 3. Título de sección de propuestas
                item {
                    Text(
                        text = stringResource(id = R.string.label_propuestas),
                        color = colors.text,
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier.padding(top = 8.dp, bottom = 2.dp)
                    )
                }

                // 4. Lista de Propuestas de Gobierno enumeradas
                itemsIndexed(candidate.proposals) { index, proposal ->
                    Card(
                        shape = RoundedCornerShape(14.dp),
                        colors = CardDefaults.cardColors(containerColor = colors.card),
                        border = BorderStroke(1.dp, colors.border),
                        elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 0.dp else 2.dp),
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Row(
                            modifier = Modifier.padding(16.dp),
                            verticalAlignment = Alignment.Top
                        ) {
                            Box(
                                modifier = Modifier
                                    .size(30.dp)
                                    .background(colors.primaryContainer, CircleShape),
                                contentAlignment = Alignment.Center
                            ) {
                                Text(
                                    text = "${index + 1}",
                                    color = Color.White,
                                    fontWeight = FontWeight.Bold,
                                    fontSize = 14.sp
                                )
                            }
                            Spacer(modifier = Modifier.width(14.dp))
                            Text(
                                text = proposal.text,
                                color = colors.text,
                                fontSize = 14.sp,
                                lineHeight = 21.sp,
                                modifier = Modifier.weight(1f)
                            )
                        }
                    }
                }
            }
        }
    }
}
