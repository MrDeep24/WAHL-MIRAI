package com.example.myapplication.ui.votaciones

import com.example.myapplication.data.models.VotingItem

enum class FiltroVotaciones {
    ACTIVAS,
    YA_PARTICIPASTE
}

data class VotacionesUiState(
    val isLoading: Boolean = false,
    val filtroActual: FiltroVotaciones = FiltroVotaciones.ACTIVAS,
    val votaciones: List<VotingItem> = emptyList(),
    val errorMessage: String? = null
)
