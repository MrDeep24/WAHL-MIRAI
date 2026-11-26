package com.example.myapplication.data.models

sealed class SessionEvent {
    object Normal : SessionEvent()
    object SesionReemplazada : SessionEvent()
    object TokenExpirado : SessionEvent()
}
