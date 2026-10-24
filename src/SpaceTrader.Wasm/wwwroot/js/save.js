window.stSave = {
    load: () => localStorage.getItem("spacetrader-save"),
    save: (json) => localStorage.setItem("spacetrader-save", json),
    clear: () => localStorage.removeItem("spacetrader-save"),
    scores: () => localStorage.getItem("spacetrader-scores"),
    saveScores: (json) => localStorage.setItem("spacetrader-scores", json)
};
