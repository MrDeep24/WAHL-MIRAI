package com.example.myapplication.data.repository

import android.content.Context
import com.example.myapplication.data.local.VotingDataStore
import com.example.myapplication.data.models.VotingItem
import com.example.myapplication.data.remote.ApiClient
import com.example.myapplication.data.remote.MobileVoteRequest
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.map

class VotingRepository(private val context: Context) {
    private val dataStore = VotingDataStore(context)

    private val votingItems = MutableStateFlow<List<VotingItem>>(emptyList())

    fun getVotingItems(): Flow<List<VotingItem>> = votingItems.asStateFlow()

    suspend fun refreshVotingItems(): Result<List<VotingItem>> {
        return try {
            val response = ApiClient.service.getEvents()
            if (response.isSuccessful) {
                val items = response.body().orEmpty().map { it.toDomain() }
                votingItems.value = items
                Result.success(items)
            } else {
                val msg = ApiClient.parseErrorMessage(
                    response.errorBody(),
                    "No se pudieron cargar las votaciones (${response.code()})."
                )
                Result.failure(Exception(msg))
            }
        } catch (e: Exception) {
            Result.failure(Exception(ApiClient.mapNetworkException(e)))
        }
    }

    suspend fun refreshVotingItemById(id: String): Result<VotingItem> {
        return try {
            val response = ApiClient.service.getEventById(id)
            if (response.isSuccessful) {
                val item = response.body()?.toDomain()
                    ?: return Result.failure(Exception("Votación no encontrada."))
                val current = votingItems.value.toMutableList()
                val idx = current.indexOfFirst { it.id == id }
                if (idx >= 0) {
                    current[idx] = item
                } else {
                    current.add(item)
                }
                votingItems.value = current
                Result.success(item)
            } else {
                val msg = ApiClient.parseErrorMessage(
                    response.errorBody(),
                    "No se pudo cargar la votación (${response.code()})."
                )
                Result.failure(Exception(msg))
            }
        } catch (e: Exception) {
            Result.failure(Exception(ApiClient.mapNetworkException(e)))
        }
    }

    suspend fun getVotingItemById(id: String): VotingItem? {
        return votingItems.value.find { it.id == id }
            ?: refreshVotingItemById(id).getOrNull()
    }

    fun getVotingItemByIdFlow(id: String): Flow<VotingItem?> {
        return votingItems.map { list ->
            list.find { it.id == id }
        }
    }

    suspend fun registerVote(votacionId: String, candidateId: String): Result<String> {
        val eventIdInt = votacionId.toIntOrNull()
            ?: return Result.failure(Exception("Identificador de votación inválido."))
        val candidateIdInt = candidateId.toIntOrNull()
            ?: return Result.failure(Exception("Identificador de candidato inválido."))

        return try {
            val response = ApiClient.service.castVote(
                MobileVoteRequest(eventId = eventIdInt, candidateId = candidateIdInt)
            )
            if (response.isSuccessful && response.body()?.success == true) {
                dataStore.markAsVoted(votacionId)
                refreshVotingItemById(votacionId)
                Result.success(response.body()?.message ?: "¡Tu voto ha sido registrado exitosamente!")
            } else {
                val msg = ApiClient.parseErrorMessage(
                    response.errorBody(),
                    "No fue posible registrar tu voto."
                )
                Result.failure(Exception(msg))
            }
        } catch (e: Exception) {
            Result.failure(Exception(ApiClient.mapNetworkException(e)))
        }
    }
}
