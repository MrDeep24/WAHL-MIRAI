package com.example.myapplication.data.repository

import com.example.myapplication.data.models.SessionEvent
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

object SessionManager {
    private val _sessionState = MutableStateFlow<SessionEvent>(SessionEvent.Normal)
    val sessionState: StateFlow<SessionEvent> = _sessionState.asStateFlow()

    @Volatile
    private var authToken: String? = null

    fun setToken(token: String?) {
        authToken = token
    }

    fun getToken(): String? = authToken

    fun clearSession() {
        authToken = null
    }

    fun emitExpired() {
        authToken = null
        _sessionState.value = SessionEvent.TokenExpirado
    }

    fun reset() {
        _sessionState.value = SessionEvent.Normal
    }
}
