"use client";

import { useState, useRef, useEffect } from "react";

const STATIONS = [
  { id: "kids", name: "ABC KIDS", desc: "Stories & Music", icon: "👧", url: "https://live-radio01.mediahubaustralia.com/XTDW/mp3/", source: "Internet" },
  { id: "jjj", name: "TRIPLE J", desc: "New Music", icon: "🎸", url: "https://live-radio01.mediahubaustralia.com/2TJW/mp3/", source: "Internet" },
  { id: "classic", name: "ABC CLASSIC", desc: "Classical Music", icon: "🎻", url: "https://live-radio01.mediahubaustralia.com/2FMW/mp3/", source: "Internet" },
];

export default function Home() {
  const [activeTab, setActiveTab] = useState<"RADIO" | "LIVE">("RADIO");
  const [playingId, setPlayingId] = useState<string | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [volume, setVolume] = useState(1.0);
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
        audioRef.current.volume = volume;
        audioRef.current.play().then(() => {
          setPlayingId(stationId);
          setIsPlaying(true);
        }).catch(err => {
          alert("Playback failed. Mobile browsers may block autoplay. Ensure you tap the play button. Error: " + err.message);
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
      audioRef.current.play().catch(err => alert("Playback error: " + err.message));
      setIsPlaying(true);
    }
  };

  const handleVolumeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = parseFloat(e.target.value);
    setVolume(val);
    if (audioRef.current) {
      audioRef.current.volume = val;
    }
  };

  return (
    <div className="app-container" style={{ paddingBottom: "120px" }}>
      <header className="header" style={{ marginBottom: "1rem" }}>
        <h1 style={{ fontSize: "1.8rem", textAlign: "left", paddingLeft: "10px" }}>DubboSDR</h1>
      </header>

      <div className="tabs" style={{ borderRadius: "12px", background: "#1a1d24", display: "flex", gap: "10px", padding: "5px" }}>
        <div 
          style={{ flex: 1, padding: "12px", textAlign: "center", borderRadius: "8px", fontWeight: "bold", background: activeTab === "RADIO" ? "#2a2d35" : "transparent", color: activeTab === "RADIO" ? "#fff" : "#8892b0" }}
          onClick={() => setActiveTab("RADIO")}
        >
          🎵 RADIO
        </div>
        <div 
          style={{ flex: 1, padding: "12px", textAlign: "center", borderRadius: "8px", fontWeight: "bold", background: activeTab === "LIVE" ? "#2a2d35" : "transparent", color: activeTab === "LIVE" ? "#fff" : "#8892b0" }}
          onClick={() => setActiveTab("LIVE")}
        >
          📡 LIVE
        </div>
      </div>

      <div style={{ marginTop: "1.5rem" }}>
        {activeTab === "RADIO" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "15px" }}>
            {STATIONS.map(station => (
              <div 
                key={station.id}
                style={{ 
                  border: `2px solid ${playingId === station.id && isPlaying ? "#4ade80" : "#2a2d35"}`, 
                  borderRadius: "16px", 
                  padding: "20px", 
                  background: "#16181d",
                  display: "flex",
                  alignItems: "center",
                  gap: "20px",
                  cursor: "pointer"
                }}
                onClick={() => handlePlay(station.id)}
              >
                <div style={{ fontSize: "2.5rem" }}>{station.icon}</div>
                <div>
                  <div style={{ fontWeight: 800, fontSize: "1.2rem", letterSpacing: "1px" }}>{station.name}</div>
                  {station.desc && <div style={{ color: "#8892b0", fontSize: "0.9rem" }}>{station.desc}</div>}
                </div>
              </div>
            ))}
          </div>
        )}

        {activeTab === "LIVE" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "15px" }}>
            <div style={{ background: "#16181d", border: "1px solid #2a2d35", borderRadius: "16px", padding: "20px" }}>
              <div style={{ display: "flex", alignItems: "center", gap: "10px", marginBottom: "5px" }}>
                <div style={{ width: "12px", height: "12px", borderRadius: "50%", background: "#f44336" }}></div>
                <strong style={{ fontSize: "1.1rem" }}>SDR computer offline</strong>
              </div>
              <div style={{ color: "#8892b0" }}>Your Internet Radio still works.</div>
            </div>

            <h3 style={{ color: "#8892b0", marginTop: "10px", fontSize: "0.9rem", textTransform: "uppercase" }}>Live Around Dubbo</h3>

            <div style={{ background: "#16181d", border: "1px solid #2a2d35", borderRadius: "16px", padding: "15px", display: "flex", alignItems: "center", gap: "15px" }}>
              <div style={{ fontSize: "1.8rem" }}>✈️</div>
              <div>
                <strong style={{ fontSize: "1.1rem" }}>Aircraft</strong>
                <div style={{ color: "#8892b0", fontSize: "0.85rem" }}>Coming next</div>
              </div>
            </div>

            <div style={{ background: "#16181d", border: "1px solid #2a2d35", borderRadius: "16px", padding: "15px", display: "flex", alignItems: "center", gap: "15px" }}>
              <div style={{ fontSize: "1.8rem" }}>🚙</div>
              <div>
                <strong style={{ fontSize: "1.1rem" }}>UHF CB</strong>
                <div style={{ color: "#8892b0", fontSize: "0.85rem" }}>SIMULATED DATA</div>
              </div>
            </div>
            
            <div style={{ background: "#16181d", border: "1px solid #2a2d35", borderRadius: "16px", padding: "15px", display: "flex", alignItems: "center", gap: "15px" }}>
              <div style={{ fontSize: "1.8rem" }}>🎙️</div>
              <div>
                <strong style={{ fontSize: "1.1rem" }}>Amateur Radio</strong>
                <div style={{ color: "#8892b0", fontSize: "0.85rem" }}>Coming soon</div>
              </div>
            </div>
          </div>
        )}
      </div>

      <div style={{
        position: "fixed", bottom: 0, left: 0, right: 0, 
        background: "#0f1115", borderTop: "1px solid #2a2d35",
        padding: "20px", display: "flex", flexDirection: "column", gap: "15px",
        transform: playingId ? "translateY(0)" : "translateY(100%)",
        transition: "transform 0.3s ease"
      }}>
        <div style={{ textAlign: "center" }}>
          <div style={{ fontSize: "0.8rem", color: "#8892b0", textTransform: "uppercase", letterSpacing: "1px" }}>NOW PLAYING</div>
          <div style={{ fontWeight: 800, fontSize: "1.3rem", marginTop: "5px" }}>{activeStation?.name || "None"}</div>
          <div style={{ display: "inline-block", background: "rgba(255,255,255,0.1)", padding: "2px 8px", borderRadius: "4px", fontSize: "0.7rem", marginTop: "5px" }}>
            {activeStation?.source === "Internet" ? "🌐 Internet" : "📡 SDR"}
          </div>
        </div>
        
        <div style={{ display: "flex", justifyContent: "center" }}>
          <button 
            onClick={togglePlay}
            style={{ 
              background: "#4ade80", color: "#000", border: "none", 
              borderRadius: "30px", padding: "12px 40px", 
              fontWeight: 800, fontSize: "1.1rem", display: "flex", alignItems: "center", gap: "10px",
              boxShadow: "0 4px 15px rgba(74, 222, 128, 0.3)"
            }}
          >
            {isPlaying ? "⏸ PAUSE" : "▶ LISTEN"}
          </button>
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: "15px", padding: "0 20px" }}>
          <span style={{ fontSize: "1.2rem" }}>🔈</span>
          <input 
            type="range" 
            min="0" max="1" step="0.05" 
            value={volume} 
            onChange={handleVolumeChange} 
            style={{ flex: 1, accentColor: "#4ade80" }}
          />
          <span style={{ fontSize: "1.2rem" }}>🔊</span>
        </div>
      </div>
    </div>
  );
}
