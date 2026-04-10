import { useState } from "react";
import "./App.css";

const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5092/api/url/shorten";

function App() {
    const [inputUrl, setInputUrl] = useState("");
    const [shortUrl, setShortUrl] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);
    const [copied, setCopied] = useState(false);

    const handleShorten = async () => {
        setError("");
        setShortUrl("");
        setCopied(false);
        setLoading(true);

        try {
            const response = await fetch(API_URL, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(inputUrl),
            });

            if (!response.ok) {
                const msg = await response.text();
                setError(msg);
                return;
            }

            const data = await response.json();
            setShortUrl(data.shortUrl);
        } catch {
            setError("Khong the ket noi den server.");
        } finally {
            setLoading(false);
        }
    };

    const handleCopy = async () => {
        if (!shortUrl) return;

        await navigator.clipboard.writeText(shortUrl);
        setCopied(true);
        window.setTimeout(() => setCopied(false), 1800);
    };

    return (
        <main className="page-shell">
            <div className="ambient ambient-left" />
            <div className="ambient ambient-right" />

            <header className="topbar">
                <div className="brand">Shrtly</div>
                <nav className="nav-links" aria-label="Primary">
                    <a href="#hero">Home</a>
                    <a href="#shortener">All links</a>
                    <a href="#shortener">Users</a>
                </nav>
                <div className="nav-actions">
                    <button type="button" className="ghost-button">Log in</button>
                    <button type="button" className="solid-button">Sign up</button>
                </div>
            </header>

            <section className="hero" id="hero">
                <div className="eyebrow">Free URL shortener</div>
                <h1>
                    Turn long links into
                    <span> clean, shareable </span>
                    ones
                </h1>
                <p className="hero-copy">
                    Paste any URL below and get a short link instantly with a cleaner,
                    calmer interface that feels closer to a polished landing page.
                </p>
            </section>

            <section className="shortener-card" id="shortener">
                <div className="card-header">
                    <p className="card-kicker">Shorten a link</p>
                    <h2>Paste your long URL to get started</h2>
                </div>

                <label className="field-label" htmlFor="url-input">
                    Your URL
                </label>

                <div className="input-stack">
                    <input
                        id="url-input"
                        className="url-input"
                        type="text"
                        placeholder="https://example.com/very-long-url-goes-here"
                        value={inputUrl}
                        onChange={(e) => setInputUrl(e.target.value)}
                    />

                    <button
                        type="button"
                        className={`submit-button${loading ? " is-loading" : ""}`}
                        onClick={handleShorten}
                        disabled={loading}
                    >
                        {loading ? "Dang xu ly..." : "Get my short link"}
                        <span aria-hidden="true">→</span>
                    </button>
                </div>

                {shortUrl && (
                    <div className="feedback-panel success-panel">
                        <div>
                            <p className="feedback-title">Short URL is ready</p>
                            <a href={shortUrl} target="_blank" rel="noreferrer">
                                {shortUrl}
                            </a>
                        </div>
                        <button
                            type="button"
                            className={`copy-button${copied ? " copied" : ""}`}
                            onClick={handleCopy}
                        >
                            {copied ? "Copied" : "Copy link"}
                        </button>
                    </div>
                )}

                {error && (
                    <div className="feedback-panel error-panel">
                        <p className="feedback-title">Request failed</p>
                        <p>{error}</p>
                    </div>
                )}
            </section>
        </main>
    );
}

export default App;
