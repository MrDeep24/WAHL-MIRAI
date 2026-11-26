package com.example.myapplication.ui.plan

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.myapplication.data.models.Candidate
import com.example.myapplication.data.models.VotingItem
import com.example.myapplication.data.repository.VotingRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class PlanGobiernoUiState(
    val isLoading: Boolean = true,
    val item: VotingItem? = null,
    val candidate: Candidate? = null
)

class PlanGobiernoViewModel(private val repository: VotingRepository) : ViewModel() {
    private val _uiState = MutableStateFlow(PlanGobiernoUiState())
    val uiState: StateFlow<PlanGobiernoUiState> = _uiState.asStateFlow()

    fun loadPlan(votacionId: String, candidateId: String) {
        viewModelScope.launch {
            repository.getVotingItemByIdFlow(votacionId).collect { item ->
                val candidate = item?.candidates?.find { it.id == candidateId }
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        item = item,
                        candidate = candidate
                    )
                }
            }
        }
    }
}
