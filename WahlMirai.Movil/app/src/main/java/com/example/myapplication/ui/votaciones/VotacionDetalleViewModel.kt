package com.example.myapplication.ui.votaciones

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.myapplication.data.models.VotacionStatus
import com.example.myapplication.data.models.VotingItem
import com.example.myapplication.data.repository.VotingRepository
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch

data class VotacionDetalleUiState(
    val isLoading: Boolean = true,
    val item: VotingItem? = null,
    val selectedCandidateId: String? = null,
    val isSubmitting: Boolean = false,
    val showConfirmDialog: Boolean = false,
    val errorMessage: String? = null,
    val voteSuccess: Boolean = false
)

class VotacionDetalleViewModel(private val repository: VotingRepository) : ViewModel() {
    private val _uiState = MutableStateFlow(VotacionDetalleUiState())
    val uiState: StateFlow<VotacionDetalleUiState> = _uiState.asStateFlow()
    private var loadJob: Job? = null

    fun loadVotingItem(votacionId: String, preselectedCandidateId: String? = null) {
        loadJob?.cancel()
        _uiState.update { state ->
            val isSameEvent = state.item?.id == votacionId
            state.copy(
                isLoading = !isSameEvent,
                item = if (isSameEvent) state.item else null,
                errorMessage = null,
                voteSuccess = false,
                selectedCandidateId = preselectedCandidateId
                    ?: if (isSameEvent) state.selectedCandidateId else null
            )
        }
        loadJob = viewModelScope.launch {
            launch {
                repository.refreshVotingItemById(votacionId).onFailure { err ->
                    if (_uiState.value.item == null) {
                        _uiState.update { it.copy(isLoading = false, errorMessage = err.message) }
                    }
                }
            }
            launch {
                while (isActive) {
                    delay(4000L)
                    val current = _uiState.value.item
                    if (current != null && current.status == VotacionStatus.ACTIVA) {
                        repository.refreshVotingItemById(votacionId)
                    } else if (current != null && current.status == VotacionStatus.FINALIZADA) {
                        break
                    }
                }
            }
            repository.getVotingItemByIdFlow(votacionId).collect { item ->
                _uiState.update { state ->
                    state.copy(
                        isLoading = if (item != null) false else state.isLoading,
                        item = item,
                        selectedCandidateId = preselectedCandidateId ?: state.selectedCandidateId
                    )
                }
            }
        }
    }

    fun stopLiveUpdates() {
        loadJob?.cancel()
        loadJob = null
    }

    fun selectCandidate(candidateId: String) {
        if (_uiState.value.item?.hasVoted == true) return
        _uiState.update { it.copy(selectedCandidateId = candidateId, errorMessage = null) }
    }

    fun openConfirmDialog() {
        if (_uiState.value.selectedCandidateId != null && _uiState.value.item?.hasVoted == false) {
            _uiState.update { it.copy(showConfirmDialog = true, errorMessage = null) }
        }
    }

    fun closeConfirmDialog() {
        _uiState.update { it.copy(showConfirmDialog = false) }
    }

    fun clearError() {
        _uiState.update { it.copy(errorMessage = null) }
    }

    fun submitVote(onSuccess: () -> Unit) {
        val candidateId = _uiState.value.selectedCandidateId ?: return
        val item = _uiState.value.item ?: return
        if (item.hasVoted || _uiState.value.isSubmitting) return

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, showConfirmDialog = false, errorMessage = null) }
            val result = repository.registerVote(item.id, candidateId)
            result.onSuccess {
                val updatedItem = repository.getVotingItemById(item.id)
                _uiState.update { it.copy(isSubmitting = false, item = updatedItem, voteSuccess = true) }
                onSuccess()
            }.onFailure { error ->
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        errorMessage = error.message ?: "No fue posible registrar tu voto."
                    )
                }
            }
        }
    }
}
