package com.example.myapplication.data.models

import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

enum class VotacionStatus {
    PROGRAMADA, INSCRIPCION, PROPUESTAS, ACTIVA, FINALIZADA, ELIMINADO
}

data class Proposal(
    val id: String,
    val text: String
)

data class Candidate(
    val id: String,
    val name: String,                  // Columna `candidates.name`
    val number: String,                // Número de tarjetón
    val grade: String,                 // Grado del candidato (`grades.name`)
    val slogan: String,                // Columna `candidates.slogan`
    val summary: String,               // Resumen descriptivo
    val proposals: List<Proposal>,     // Registros de la tabla `candidate_proposals`
    val photoUrl: String? = null,      // Columna `candidates.photo_url`
    val pdfUrl: String? = null,        // Columna `candidates.government_plan_url`
    val isWhiteVote: Boolean = false,  // Columna `candidates.is_blank_vote`
    val status: String = "APROBADO",   // Columna `candidates.status` ENUM('PENDIENTE','APROBADO','RECHAZADO','RETIRADO')
    val votes: Int = 0,                // Vista `vw_vote_counts.total_votes`
    val percentage: Float = 0f,        // Vista `vw_vote_counts.vote_percentage`
    val isSelected: Boolean = false
)

data class VotingItem(
    val id: String,
    val title: String,
    val description: String? = null,
    val electionType: String = "PERSONAS", // 'PERSONAS' o 'TEMAS'
    
    // Etapas basadas en wahl_mirai_db_v3_2_completo.sql
    val registrationStartDate: Long,
    val registrationEndDate: Long,
    
    val proposalsStartDate: Long,
    val proposalsEndDate: Long,
    
    val votingStartDate: Long,
    val votingEndDate: Long,
    
    val candidates: List<Candidate>,
    val hasVoted: Boolean = false,
    val serverStatus: String? = null
) {
    val status: VotacionStatus
        get() {
            val rawStatus = serverStatus
            if (!rawStatus.isNullOrBlank()) {
                runCatching { return VotacionStatus.valueOf(rawStatus.uppercase()) }
            }
            val now = System.currentTimeMillis()
            return when {
                now < registrationStartDate -> VotacionStatus.PROGRAMADA
                now in registrationStartDate..registrationEndDate -> VotacionStatus.INSCRIPCION
                now in proposalsStartDate..proposalsEndDate -> VotacionStatus.PROPUESTAS
                now in votingStartDate..votingEndDate -> VotacionStatus.ACTIVA
                else -> VotacionStatus.FINALIZADA
            }
        }

    val timeDisplay: String
        get() {
            val now = System.currentTimeMillis()
            return when (status) {
                VotacionStatus.PROGRAMADA -> {
                    val diff = registrationStartDate - now
                    formatDiff(diff, "Inscripciones inician en")
                }
                VotacionStatus.INSCRIPCION -> {
                    val diff = registrationEndDate - now
                    formatDiff(diff, "Inscripciones cierran en")
                }
                VotacionStatus.PROPUESTAS -> {
                    val diff = proposalsEndDate - now
                    formatDiff(diff, "Propuestas cierran en")
                }
                VotacionStatus.ACTIVA -> {
                    val diff = votingEndDate - now
                    formatDiff(diff, "Votación cierra en")
                }
                VotacionStatus.FINALIZADA, VotacionStatus.ELIMINADO -> {
                    val sdf = SimpleDateFormat("dd/MM/yyyy", Locale.getDefault())
                    "Finalizó el ${sdf.format(Date(votingEndDate))}"
                }
            }
        }
        
    private fun formatDiff(diff: Long, prefix: String): String {
        val days = diff / (1000 * 60 * 60 * 24)
        val hours = (diff / (1000 * 60 * 60)) % 24
        return if (days > 0) "$prefix ${days}d ${hours}h" else "$prefix ${hours}h"
    }
}

// Registro inmutable de voto (Tabla `votes` en SQL) - SIN user_id para secreto del voto (RN-3)
data class VoteRecord(
    val id: Long,
    val votingEventId: String,
    val candidateId: String,
    val votedAt: Long = System.currentTimeMillis(),
    val voteHash: String
)

// Control anti-duplicado de participación (Tabla `event_participations` en SQL)
data class EventParticipation(
    val id: Long,
    val userId: String,
    val votingEventId: String,
    val participatedAt: Long = System.currentTimeMillis()
)
