"use client";

import { useState, useRef, useEffect } from "react";

const STATIONS = [
  { id: "jjj", name: "triple j", icon: "🥁", url: "https://live-radio01.mediahubaustralia.com/2TJW/mp3/", source: "Internet" },
  { id: "kids", name: "ABC Kids", icon: "🧸", url: "https://live-radio01.mediahubaustralia.com/XTDW/mp3/", source: "Internet" },
  { id: "classic", name: "Classic", icon: "🎻", url: "https://live-radio01.mediahubaustralia.com/2FMW/mp3/", source: "Internet" },
  // Placeholders for Dubbo local that don't have direct streams yet
  { id: "triplem", name: "Triple M", icon: "🎸", url: "", source: "Hybrid" },
  { id: "zoo", name: "Zoo FM", icon: "🦒", url: "", source: "Hybrid" }
];

export default function Home() {
  const [activeTab, setActiveTab] = useState<"RADIO" | "LIVE">("RADIO");
  const [playingId, setPlayingId] = useState<string | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const audioRef = useRef<HTMLAudioElement | null>(null);

  const activeStation = STATIONS.find(s => s.id === playingId);

  useEffect(() => {
    audioRef.current = new Audio();
    return () => {
      audioRef.current?.pause();
      audioRef.current = null;
    };
  }, []);

  const handlePlay = (stationId: string) => {
    const station = STATIONS.find(s => s.id === stationId);
    if (!station || !station.url) {
      alert("No stream URL available for this station yet.");
      return;
    }

    if (playingId === stationId && isPlaying) {
      audioRef.current?.pause();
      setIsPlaying(false);
    } else {
      if (audioRef.current) {
        audioRef.current.src = station.url;
        audioRef.current.play().then(() => {
          setPlayingId(stationId);
          setIsPlaying(true);
        }).catch(err => {
          alert("Playback failed: " + err.message);
        });
      }
    }
  };

  const togglePlay = () => {
    if (!audioRef.current) return;
    if (isPlaying) {
      audioRef.current.pause();
      setIsPlaying(false);
    } else {
      audioRef.current.play();
      setIsPlaying(true);
    }
  };

  return (
    <div className="app-container">
      <header className="header">
        <h1>DubboSDR</h1>
      </header>

      <div className="tabs">
        <div 
          className={`tab ${activeTab === "RADIO" ? "active" : ""}`}
          onClick={() => setActiveTab("RADIO")}
        >
          🎵 RADIO
        </div>
        <div 
          className={`tab ${activeTab === "LIVE" ? "active" : ""}`}
          onClick={() => setActiveTab("LIVE")}
        >
          📡 LIVE
        </div>
      </div>

      {activeTab === "RADIO" && (
        <div className="station-grid">
          {STATIONS.map(station => (
            <div 
              key={station.id}
              className={`station-card ${playingId === station.id && isPlaying ? "playing" : ""}`}
              onClick={() => handlePlay(station.id)}
            >
              <div className="source-badge">{station.source === "Internet" ? "🌐 WEB" : "📡 SDR"}</div>
              <div className="station-icon">{station.icon}</div>
              <div className="station-name">{station.name}</div>
            </div>
          ))}
        </div>
      )}

      {activeTab === "LIVE" && (
        <div>
          <div className="sdr-status">
            <div className="dot"></div>
            <div>
              <strong>○ SDR computer offline</strong>
              <div style={{fontSize: "0.85rem", color: "var(--text-muted)", marginTop: "4px"}}>
                Internet Radio still available.
              </div>
            </div>
          </div>

          <h3 style={{marginBottom: "1rem", color: "var(--text-muted)", fontSize: "0.9rem", textTransform: "uppercase", letterSpacing: "1px"}}>Live Around Dubbo</h3>

          <div className="live-card">
            <div className="live-card-icon">✈️</div>
            <div>
              <strong>Aircraft</strong>
              <div style={{fontSize: "0.85rem", color: "var(--text-muted)"}}>Coming next</div>
            </div>
          </div>

          <div className="live-card">
            <div className="live-card-icon">🚙</div>
            <div>
              <strong>UHF CB</strong>
              <div style={{fontSize: "0.85rem", color: "var(--text-muted)"}}>Coming soon</div>
            </div>
          </div>

          <div className="live-card">
            <div className="live-card-icon">🎙️</div>
            <div>
              <strong>Amateur Radio</strong>
              <div style={{fontSize: "0.85rem", color: "var(--text-muted)"}}>Coming soon</div>
            </div>
          </div>
          
          <div className="live-card">
            <div className="live-card-icon">🌡️</div>
            <div>
              <strong>Nearby Sensors</strong>
              <div style={{fontSize: "0.85rem", color: "var(--text-muted)"}}>Coming soon</div>
            </div>
          </div>
        </div>
      )}

      <div className={`now-playing ${playingId ? "visible" : ""}`}>
        <div>
          <div style={{fontSize: "0.8rem", color: "var(--text-muted)", textTransform: "uppercase"}}>Now Playing</div>
          <div style={{fontWeight: 600, fontSize: "1.2rem"}}>{activeStation?.name || "None"}</div>
        </div>
        <button className="play-btn" onClick={togglePlay}>
          {isPlaying ? "⏸" : "▶"}
        </button>
      </div>
    </div>
  );
}
