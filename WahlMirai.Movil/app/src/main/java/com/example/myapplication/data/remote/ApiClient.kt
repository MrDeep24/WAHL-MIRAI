package com.example.myapplication.data.remote

import com.example.myapplication.BuildConfig
import com.example.myapplication.data.repository.SessionManager
import com.google.gson.Gson
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.ResponseBody
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.io.IOException
import java.net.SocketTimeoutException
import java.net.UnknownHostException
import java.util.concurrent.TimeUnit
import javax.net.ssl.SSLException

object ApiClient {

    private val gson = Gson()

    private val authInterceptor = Interceptor { chain ->
        val original = chain.request()
        val builder = original.newBuilder()
            .header("Accept", "application/json")

        val token = SessionManager.getToken()
        if (!token.isNullOrBlank()) {
            builder.header("Authorization", "Bearer $token")
        }

        val response = chain.proceed(builder.build())

        // Si el backend rechaza el token (sesión expirada o reemplazada en otro dispositivo - RN-14)
        val isLoginRequest = original.url.encodedPath.endsWith("/api/mobile/login", ignoreCase = true)
        if (response.code == 401 && !isLoginRequest) {
            SessionManager.emitExpired()
        }

        response
    }

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = if (BuildConfig.DEBUG) {
            HttpLoggingInterceptor.Level.BODY
        } else {
            HttpLoggingInterceptor.Level.NONE
        }
    }

    private val okHttpClient: OkHttpClient by lazy {
        OkHttpClient.Builder()
            .addInterceptor(authInterceptor)
            .addInterceptor(loggingInterceptor)
            .connectTimeout(15, TimeUnit.SECONDS)
            .readTimeout(20, TimeUnit.SECONDS)
            .writeTimeout(20, TimeUnit.SECONDS)
            .build()
    }

    val service: WahlMiraiApiService by lazy {
        Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create(gson))
            .build()
            .create(WahlMiraiApiService::class.java)
    }

    fun parseErrorMessage(errorBody: ResponseBody?, defaultMessage: String): String {
        val raw = try {
            errorBody?.string()
        } catch (_: Exception) {
            null
        }
        if (raw.isNullOrBlank()) return defaultMessage

        return try {
            val parsed = gson.fromJson(raw, MobileErrorResponse::class.java)
            parsed?.message?.takeIf { it.isNotBlank() } ?: defaultMessage
        } catch (_: Exception) {
            defaultMessage
        }
    }

    fun mapNetworkException(throwable: Throwable): String {
        return when (throwable) {
            is UnknownHostException -> "No se pudo conectar con el servidor (${BuildConfig.API_BASE_URL}). Verifica tu conexión a internet o el túnel de Cloudflare."
            is SocketTimeoutException -> "El servidor tardó demasiado en responder. Intenta nuevamente."
            is SSLException -> "Error de seguridad SSL/TLS al conectar con el dominio."
            is IOException -> "Error de red al comunicarse con el servidor. Verifica tu conexión."
            else -> throwable.message ?: "Error inesperado de comunicación."
        }
    }
}
