import { useState } from "react";

function App() {
    const [inputUrl, setInputUrl] = useState("");
    const [shortUrl, setShortUrl] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    const handleShorten = async () => {
        setError("");
        setShortUrl("");
        setLoading(true);

        try {
            const response = await fetch("http://localhost:5092/api/url/shorten", {
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
            setError("Không thể kết nối đến server.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={styles.container}>
            <h1 style={styles.title}>🔗 URL Shortener</h1>

            <input
                style={styles.input}
                type="text"
                placeholder="Nhập URL dài vào đây..."
                value={inputUrl}
                onChange={(e) => setInputUrl(e.target.value)}
            />

            <button style={styles.button} onClick={handleShorten} disabled={loading}>
                {loading ? "Đang xử lý..." : "Rút gọn"}
            </button>

            {shortUrl && (
                <div style={styles.result}>
                    <p>✅ URL rút gọn:</p>
                    <a href={shortUrl} target="_blank" rel="noreferrer">
                        {shortUrl}
                    </a>
                    <button
                        style={styles.copy}
                        onClick={() => navigator.clipboard.writeText(shortUrl)}
                    >
                        Copy
                    </button>
                </div>
            )}

            {error && <p style={styles.error}>❌ {error}</p>}
        </div>
    );
}

const styles = {
    container: {
        maxWidth: 500,
        margin: "80px auto",
        padding: 32,
        borderRadius: 12,
        boxShadow: "0 4px 20px rgba(0,0,0,0.1)",
        fontFamily: "sans-serif",
        textAlign: "center",
    },
    title: {
        fontSize: 28,
        marginBottom: 24,
    },
    input: {
        width: "100%",
        padding: "10px 14px",
        fontSize: 16,
        borderRadius: 8,
        border: "1px solid #ccc",
        marginBottom: 12,
        boxSizing: "border-box",
    },
    button: {
        padding: "10px 28px",
        fontSize: 16,
        backgroundColor: "#4f46e5",
        color: "white",
        border: "none",
        borderRadius: 8,
        cursor: "pointer",
    },
    result: {
        marginTop: 24,
        padding: 16,
        backgroundColor: "#f0fdf4",
        borderRadius: 8,
    },
    copy: {
        marginLeft: 10,
        padding: "4px 12px",
        cursor: "pointer",
        borderRadius: 6,
        border: "1px solid #ccc",
    },
    error: {
        color: "red",
        marginTop: 16,
    },
};

export default App;