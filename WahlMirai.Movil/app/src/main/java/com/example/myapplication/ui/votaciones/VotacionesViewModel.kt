package com.example.myapplication.ui.votaciones

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.myapplication.data.repository.VotingRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class VotacionesViewModel(private val repository: VotingRepository) : ViewModel() {
    private val _uiState = MutableStateFlow(VotacionesUiState())
    val uiState: StateFlow<VotacionesUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            repository.getVotingItems().collect { items ->
                _uiState.update { it.copy(votaciones = items) }
            }
        }
    }

    fun cargarVotaciones() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, errorMessage = null) }
            val result = repository.refreshVotingItems()
            result.onSuccess {
                _uiState.update { it.copy(isLoading = false, errorMessage = null) }
            }.onFailure { error ->
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        errorMessage = error.message ?: "Error al cargar las votaciones"
                    )
                }
            }
        }
    }

    fun setFiltro(filtro: FiltroVotaciones) {
        _uiState.update { it.copy(filtroActual = filtro) }
    }
}
