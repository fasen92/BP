export async function fetchPoints(endpoint, params) {
    const baseUrl = window.apiBaseUrl || "http://localhost:7147/api/Wifi/";
    const query = new URLSearchParams(params).toString();
    const url = `${baseUrl}${endpoint}?${query}`;
    try {
        const response = await fetch(url);
        if (!response.ok) {
            console.error("Error fetching points:", response.statusText);
            return [];
        }
        return await response.json();
    } catch (error) {
        console.error("API call error:", error);
        return [];
    }
}

export async function fetchLocationsByBssid(bssid) {
    const baseUrl = window.apiBaseUrl || "http://localhost:7147/api/Wifi/";
    const url = `${baseUrl}locations?bssid=${encodeURIComponent(bssid)}`;
    try {
        const response = await fetch(url);
        if (!response.ok) {
            console.error("Error fetching locations:", response.statusText);
            return [];
        }
        return await response.json();
    } catch (error) {
        console.error("API call error:", error);
        return [];
    }
}