package com.example.myapplication.data.repository

import com.example.myapplication.data.models.UserData
import com.example.myapplication.data.remote.ApiClient
import com.example.myapplication.data.remote.MobileLoginRequest
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.map

interface UserRepository {
    suspend fun getUserByDocument(documento: String): UserData?
    fun getUserByDocumentFlow(documento: String): Flow<UserData?>
    suspend fun verifyAndAuthenticateUser(documento: String, contrasena: String): Result<UserData>
    suspend fun updateUser(userData: UserData)
    suspend fun logout()
}

class UserRepositoryImpl : UserRepository {
    private val usuariosState = MutableStateFlow<Map<String, UserData>>(emptyMap())

    override suspend fun getUserByDocument(documento: String): UserData? {
        usuariosState.value[documento]?.let { return it }
        return try {
            val response = ApiClient.service.getProfile()
            if (response.isSuccessful) {
                val user = response.body()?.toDomain()
                if (user != null) {
                    updateUser(user)
                }
                user
            } else {
                null
            }
        } catch (_: Exception) {
            null
        }
    }

    override fun getUserByDocumentFlow(documento: String): Flow<UserData?> {
        return usuariosState.map { map -> map[documento] }
    }

    override suspend fun verifyAndAuthenticateUser(documento: String, contrasena: String): Result<UserData> {
        return try {
            val response = ApiClient.service.login(
                MobileLoginRequest(document = documento.trim(), password = contrasena)
            )

            if (response.isSuccessful) {
                val body = response.body()
                    ?: return Result.failure(Exception("Respuesta vacía del servidor."))

                SessionManager.reset()
                SessionManager.setToken(body.token)

                val user = body.user.toDomain()
                updateUser(user)
                Result.success(user)
            } else {
                val defaultMsg = when (response.code()) {
                    401 -> "Documento o contraseña incorrectos, o cuenta inactiva."
                    429 -> "Demasiados intentos de inicio de sesión. Espera un minuto."
                    else -> "Error del servidor (${response.code()})."
                }
                val serverMsg = ApiClient.parseErrorMessage(response.errorBody(), defaultMsg)
                Result.failure(Exception(serverMsg))
            }
        } catch (e: Exception) {
            Result.failure(Exception(ApiClient.mapNetworkException(e)))
        }
    }

    override suspend fun updateUser(userData: UserData) {
        val current = usuariosState.value.toMutableMap()
        current[userData.documento] = userData
        usuariosState.value = current
    }

    override suspend fun logout() {
        try {
            if (!SessionManager.getToken().isNullOrBlank()) {
                ApiClient.service.logout()
            }
        } catch (_: Exception) {
            // Ignoramos fallos de red al cerrar sesión localmente
        } finally {
            SessionManager.clearSession()
            usuariosState.value = emptyMap()
        }
    }
}
