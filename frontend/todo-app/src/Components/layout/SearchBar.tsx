import { useEffect, useState } from "react";

interface SearchBarProps {
    onSearch: (value: string) => void;
}

function SearchBar({ onSearch }: SearchBarProps) {
    const [inputValue, setInputValue] = useState("");

    // debounce typing BEFORE notifying parent
    useEffect(() => {
        const timeout = setTimeout(() => {
            onSearch(inputValue);
        }, 300);

        return () => clearTimeout(timeout);
    }, [inputValue, onSearch]);

    return (
        <div className="row justify-content-center mb-4">
            <div className="col-md-6">
                <input
                    type="text"
                    className="form-control"
                    placeholder="Search todos..."
                    value={inputValue}
                    onChange={(e) => setInputValue(e.target.value)}
                />
            </div>
        </div>
    );
}

export default SearchBar;
