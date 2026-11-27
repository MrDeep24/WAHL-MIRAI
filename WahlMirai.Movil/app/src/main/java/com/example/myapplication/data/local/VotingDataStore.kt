package com.example.myapplication.data.local

import android.content.Context
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

val Context.dataStore by preferencesDataStore(name = "voting_prefs")

class VotingDataStore(private val context: Context) {

    private val darkThemeKey = booleanPreferencesKey("is_dark_theme")

    val isDarkTheme: Flow<Boolean> = context.dataStore.data.map { prefs ->
        prefs[darkThemeKey] ?: false
    }

    suspend fun setDarkTheme(isDark: Boolean) {
        context.dataStore.edit { prefs ->
            prefs[darkThemeKey] = isDark
        }
    }

    fun hasUserVoted(votacionId: String): Flow<Boolean> {
        val key = booleanPreferencesKey("voted_$votacionId")
        return context.dataStore.data.map { prefs ->
            prefs[key] ?: false
        }
    }

    suspend fun markAsVoted(votacionId: String) {
        val key = booleanPreferencesKey("voted_$votacionId")
        context.dataStore.edit { prefs ->
            prefs[key] = true
        }
    }
}
