from flask import Flask

app = Flask(__name__)


@app.get("/")
def health():
    return {"status": "ok", "service": "devsecops-project"}


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5001)
